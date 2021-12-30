namespace SqlLiteExample.Testing
{
    public class Person
    {
        private int? _id;
        private string _name;

        public int? id
        {
            get => _id;
            set => _id = value;
        }
        public string name
        {
            get => _name;
            set => _name = value;
        }
    }
}
