namespace GenericDao.Models
{
    public class SetCommand
    {
        public string ColumnName;
        public object Value;

        public SetCommand(string columnName, string value)
        {
            ColumnName = columnName;
            Value = value;
        }
    }
}
