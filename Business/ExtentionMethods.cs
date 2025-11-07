using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Business
{
    public static class ExtentionMethods
    {
        public static IOrderedQueryable<T> OrderByColumn<T>(this IQueryable<T> source, string columnPath)
        => source.OrderByColumnUsing(columnPath, "OrderBy");

        public static IOrderedQueryable<T> OrderByColumnDescending<T>(this IQueryable<T> source, string columnPath)
            => source.OrderByColumnUsing(columnPath, "OrderByDescending");

        public static IOrderedQueryable<T> ThenByColumn<T>(this IOrderedQueryable<T> source, string columnPath)
            => source.OrderByColumnUsing(columnPath, "ThenBy");

        public static IOrderedQueryable<T> ThenByColumnDescending<T>(this IOrderedQueryable<T> source, string columnPath)
            => source.OrderByColumnUsing(columnPath, "ThenByDescending");

        private static IOrderedQueryable<T> OrderByColumnUsing<T>(this IQueryable<T> source, string columnPath, string method)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(T), "item");
            Expression member = columnPath
                .Split('.')
                .Aggregate((Expression)parameter, Expression.PropertyOrField);

            LambdaExpression keySelector = Expression.Lambda(member, parameter);

            MethodCallExpression methodCall = Expression.Call(typeof(Queryable), method, new[]
                    { parameter.Type, member.Type },
                source.Expression, Expression.Quote(keySelector));

            return (IOrderedQueryable<T>)source.Provider.CreateQuery(methodCall);
        }



        public static IQueryable<T> FilterByColumnEquality<T>(this IQueryable<T> source, string columnPath, object? value, bool isEqual)
        {
            // Guard clauses
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrEmpty(columnPath)) throw new ArgumentException("Column path cannot be null or empty.", nameof(columnPath));

            // Create parameter expression (e.g., 'item' in 'item => item.Property')
            ParameterExpression parameter = Expression.Parameter(typeof(T), "item");

            // Build property access expression (e.g., 'item.Property' or 'item.Address.City')
            Expression member = columnPath
                .Split('.')
                .Aggregate((Expression)parameter, Expression.PropertyOrField);

            // Get the target type
            Type memberType = member.Type;
            object? convertedValue;

            // Handle nullable types
            Type? underlyingType = Nullable.GetUnderlyingType(memberType);
            if (underlyingType != null)
            {
                if (value == null)
                    // Nullable field can accept null
                    convertedValue = null;

                else if (value is string stringValue && underlyingType == typeof(int))
                    // Parse string to int for int? fields
                    convertedValue = string.IsNullOrEmpty(stringValue) ? null : int.Parse(stringValue);

                else
                    // Convert to the underlying type
                    convertedValue = Convert.ChangeType(value, underlyingType);
            }
            // Non-nullable type
            else
                convertedValue = Convert.ChangeType(value, memberType);

            // Create equality expression (e.g., 'item.Property == value')
            Expression constant = Expression.Constant(convertedValue, memberType);
            Expression equality;
            if (isEqual)
                equality = Expression.Equal(member, constant);
            else
                equality = Expression.NotEqual(member, constant);


            // Create lambda expression (e.g., 'item => item.Property == value')
            LambdaExpression lambda = Expression.Lambda(equality, parameter);

            // Build the Where method call
            MethodCallExpression methodCall = Expression.Call(
                typeof(Queryable),
                "Where",
                new[] { typeof(T) },
                source.Expression,
                Expression.Quote(lambda));

            // Execute the query
            return source.Provider.CreateQuery<T>(methodCall);
        }


        public static IQueryable<T> FilterByColumnContains<T>(this IQueryable<T> source, string columnPath, string value)
        {
            // Guard clauses
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrEmpty(columnPath)) throw new ArgumentException("Column path cannot be null or empty.", nameof(columnPath));
            if (value == null) throw new ArgumentNullException(nameof(value), "Value cannot be null for Contains filter.");

            // Create parameter expression (e.g., 'item' in 'item => item.Property')
            ParameterExpression parameter = Expression.Parameter(typeof(T), "item");

            // Build property access expression (e.g., 'item.Property' or 'item.Address.City')
            Expression member = columnPath
                .Split('.')
                .Aggregate((Expression)parameter, Expression.PropertyOrField);

            // Ensure the property is a string
            Type memberType = member.Type;
            if (memberType != typeof(string))
            {
                throw new ArgumentException($"Contains filter is only supported for string properties. Property '{columnPath}' is of type '{memberType.Name}'.", nameof(columnPath));
            }

            // Create constant expression for the value
            Expression constant = Expression.Constant(value, typeof(string));

            // Create Contains method call (e.g., 'item.Property.ToLower().Contains(value)')
            //MethodInfo toLowerMethod = typeof(string).GetMethod("ToLower", [])!;
            //Expression toLowerExpression = Expression.Call(member, toLowerMethod);
            MethodInfo containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
            Expression containsExpression = Expression.Call(member, containsMethod, constant);

            // Create lambda expression (e.g., 'item => item.Property.Contains(value)')
            LambdaExpression lambda = Expression.Lambda(containsExpression, parameter);

            // Build the Where method call
            MethodCallExpression methodCall = Expression.Call(
                typeof(Queryable),
                "Where",
                new[] { typeof(T) },
                source.Expression,
                Expression.Quote(lambda));

            // Execute the query
            return source.Provider.CreateQuery<T>(methodCall);
        }


        public static IQueryable<T> FilterByColumnIn<T>(this IQueryable<T> source, string columnPath, System.Collections.IEnumerable values)
        {
            // Guard clauses
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrEmpty(columnPath)) throw new ArgumentException("Column path cannot be null or empty.", nameof(columnPath));
            if (values == null) throw new ArgumentNullException(nameof(values));

            // If no values, return empty query (IN with empty list returns no results)
            if (!values.Cast<object>().Any()) return source.Where(x => false);

            // Create parameter expression
            ParameterExpression parameter = Expression.Parameter(typeof(T), "item");

            // Build member access
            Expression member = columnPath
                .Split('.')
                .Aggregate((Expression)parameter, Expression.PropertyOrField);

            Type memberType = member.Type;
            Type? underlyingType = Nullable.GetUnderlyingType(memberType);
            Type conversionType = underlyingType ?? memberType;

            // Create list of converted values
            Type listType = typeof(List<>).MakeGenericType(memberType);
            object listInstance = Activator.CreateInstance(listType)!;
            MethodInfo addMethod = listType.GetMethod("Add")!;

            foreach (object? v in values)
            {
                object? converted;
                if (v == null)
                {
                    if (underlyingType == null)
                        throw new ArgumentException($"Null value provided for non-nullable property '{columnPath}' of type '{memberType.Name}'.");
                    converted = null;
                }
                else if (conversionType == typeof(Guid) && v is string stringValue)
                {
                    // Handle string-to-Guid conversion
                    converted = Guid.TryParse(stringValue, out Guid guidValue) ? guidValue : throw new ArgumentException($"Value '{stringValue}' is not a valid Guid for property '{columnPath}'.");
                }
                else
                {
                    try
                    {
                        converted = Convert.ChangeType(v, conversionType);
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException($"Cannot convert value '{v}' to type '{conversionType.Name}' for property '{columnPath}'.", ex);
                    }
                }

                addMethod.Invoke(listInstance, new[] { converted });
            }

            // Create constant for the list
            Expression constantCollection = Expression.Constant(listInstance, listType);

            // Get Enumerable.Contains method
            MethodInfo containsMethod = typeof(Enumerable)
                .GetMethods()
                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(memberType);

            // Create Contains call: Enumerable.Contains(list, member)
            Expression containsExpression = Expression.Call(null, containsMethod, constantCollection, member);

            // Create lambda
            LambdaExpression lambda = Expression.Lambda(containsExpression, parameter);

            // Build Where
            MethodCallExpression methodCall = Expression.Call(
                typeof(Queryable),
                "Where",
                new[] { typeof(T) },
                source.Expression,
                Expression.Quote(lambda));

            return source.Provider.CreateQuery<T>(methodCall);
        }

        public static IQueryable<T> FilterByColumnDateBetween<T>(this IQueryable<T> source, string columnPath, object startValue, object endValue)
        {
            // Guard clauses
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrEmpty(columnPath)) throw new ArgumentException("Column path cannot be null or empty.", nameof(columnPath));
            if (startValue == null) throw new ArgumentNullException(nameof(startValue), "Start value cannot be null.");
            if (endValue == null) throw new ArgumentNullException(nameof(endValue), "End value cannot be null.");

            // Create parameter expression (e.g., 'item' in 'item => item.Property')
            ParameterExpression parameter = Expression.Parameter(typeof(T), "item");

            // Build property access expression (e.g., 'item.Property' or 'item.Address.City')
            Expression member = columnPath
                .Split('.')
                .Aggregate((Expression)parameter, Expression.PropertyOrField);

            // Get the target type
            Type memberType = member.Type;
            Type? underlyingType = Nullable.GetUnderlyingType(memberType);
            Type conversionType = underlyingType ?? memberType;

            // Validate member type is DateTime or DateTime?
            if (conversionType != typeof(DateTime))
            {
                throw new ArgumentException($"Between filter is only supported for DateTime properties. Property '{columnPath}' is of type '{memberType.Name}'.");
            }

            // Convert start and end values
            DateTime startDate;
            DateTime endDate;

            try
            {
                startDate = startValue is string stringStart
                    ? DateTime.Parse(stringStart)
                    : Convert.ToDateTime(startValue);
                endDate = endValue is string stringEnd
                    ? DateTime.Parse(stringEnd)
                    : Convert.ToDateTime(endValue);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Cannot convert startValue '{startValue}' or endValue '{endValue}' to DateTime.", ex);
            }

            // Ensure startDate <= endDate
            if (startDate > endDate)
            {
                throw new ArgumentException("Start date must be less than or equal to end date.");
            }

            // Create expressions for >= startDate and <= endDate
            Expression startConstant = Expression.Constant(startDate, memberType);
            Expression endConstant = Expression.Constant(endDate, memberType);

            Expression greaterThanOrEqual = Expression.GreaterThanOrEqual(member, startConstant);
            Expression lessThanOrEqual = Expression.LessThanOrEqual(member, endConstant);

            // Combine with AND
            Expression andExpression = Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);

            // Create lambda expression (e.g., 'item => item.Property >= startDate && item.Property <= endDate')
            LambdaExpression lambda = Expression.Lambda(andExpression, parameter);

            // Build the Where method call
            MethodCallExpression methodCall = Expression.Call(
                typeof(Queryable),
                "Where",
                new[] { typeof(T) },
                source.Expression,
                Expression.Quote(lambda));

            // Execute the query
            return source.Provider.CreateQuery<T>(methodCall);
        }
    }
}
