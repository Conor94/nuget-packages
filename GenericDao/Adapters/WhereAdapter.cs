using GenericDAO.Enums;
using System;

namespace GenericDAO.Adapters
{
    public class WhereAdapter
    {
        public string ConvertOperator(WhereOperator op)
        {
            switch (op)
            {
                case WhereOperator.Equal:
                    return "=";
                case WhereOperator.GreaterThan:
                    return ">";
                case WhereOperator.LessThan:
                    return "<";
                case WhereOperator.GreaterThanOrEqual:
                    return ">=";
                case WhereOperator.LessThanOrEqual:
                    return "<=";
                case WhereOperator.NotEqual:
                    return "!=";
                case WhereOperator.Like:
                    return "LIKE";
                default:
                    throw new Exception($"No conversion exists for the given operator. This is an internal error in the {nameof(WhereAdapter)}.");
            }
        }
    }
}
