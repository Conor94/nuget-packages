using SqlLiteExample.Enums;

namespace SqlLiteExample.Models
{
    public struct OrderBy
    {
        public string[] Columns;
        public OrderByDirection Direction;
    }
}