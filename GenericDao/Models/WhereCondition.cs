using GenericDAO.Enums;

namespace GenericDAO.Models
{
    public class WhereCondition
    {
        public object LeftSide;
        public object RightSide;
        public WhereOperator ComparisonOperator;

        public WhereCondition(object leftSide, object rightSide, WhereOperator comparisonOperator = WhereOperator.Equal)
        {
            LeftSide = leftSide;
            RightSide = rightSide;
            ComparisonOperator = comparisonOperator;
        }
    }
}