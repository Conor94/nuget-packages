using GenericDao.Enums;

namespace GenericDao.Models
{
    public class OrderBy
    {
        public string[] Columns;
        public OrderByDirection Direction;

        public OrderBy(string[] columns, OrderByDirection direction = OrderByDirection.ASC)
        {
            Columns = columns;
            Direction = direction;
        }
    }
}