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

            System.Linq.Expressions.LambdaExpression keySelector = Expression.Lambda(member, parameter);

            MethodCallExpression methodCall = Expression.Call(typeof(Queryable), method, new[]
                    { parameter.Type, member.Type },
                source.Expression, Expression.Quote(keySelector));

            return (IOrderedQueryable<T>)source.Provider.CreateQuery(methodCall);
        }




        //public static IQueryable<T> FilterEqualsByColumn<T>(this IQueryable<T> source, string columnPath, object value)
        //{
        //    // Guard clauses
        //    if (source == null) throw new ArgumentNullException(nameof(source));
        //    if (string.IsNullOrEmpty(columnPath)) throw new ArgumentException("Column path cannot be null or empty.", nameof(columnPath));

        //    // Create parameter expression (e.g., 'item' in 'item => item.Property')
        //    ParameterExpression parameter = Expression.Parameter(typeof(T), "item");

        //    // Build property access expression (e.g., 'item.Property' or 'item.Address.City')
        //    Expression member = columnPath
        //        .Split('.')
        //        .Aggregate((Expression)parameter, Expression.PropertyOrField);

        //    // Convert the value to the correct type (if necessary)
        //    Type memberType = member.Type;
        //    object convertedValue = Convert.ChangeType(value, memberType);

        //    // Create equality expression (e.g., 'item.Property == value')
        //    Expression constant = Expression.Constant(convertedValue, memberType);
        //    Expression equality = Expression.Equal(member, constant);

        //    // Create lambda expression (e.g., 'item => item.Property == value')
        //    LambdaExpression lambda = Expression.Lambda(equality, parameter);

        //    // Build the Where method call
        //    MethodCallExpression methodCall = Expression.Call(
        //        typeof(Queryable),
        //        "Where",
        //        new[] { typeof(T) },
        //        source.Expression,
        //        Expression.Quote(lambda));

        //    // Execute the query
        //    return source.Provider.CreateQuery<T>(methodCall);
        //}
        public static IQueryable<T> FilterByColumnEquality<T>(this IQueryable<T> source, string columnPath, object? value,bool isEqual)
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
            if(isEqual)
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

            // Create Contains method call (e.g., 'item.Property.Contains(value)')
            MethodInfo containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
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


    }
}
