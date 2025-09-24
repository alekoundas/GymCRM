namespace Core.Enums
{
    public enum DataTableFiltersEnum
    {
        startsWith,
        contains,
        notContains,
        endsWith,
        equals,
        notEquals,
        notIn,
        lt,
        lte,
        gt,
        gte,
        between,
        dateIs,
        dateIsNot,
        dateBefore,
        dateAfter,
        custom,
        @in // Use of @ prefix to escape the reserved keyword 'in'

    }
}
