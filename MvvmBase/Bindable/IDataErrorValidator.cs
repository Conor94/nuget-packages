namespace MvvmBase.Bindable
{
    public interface IDataErrorValidator
    {
        bool Invoke(object value, out string errorMessage);
    }
}
