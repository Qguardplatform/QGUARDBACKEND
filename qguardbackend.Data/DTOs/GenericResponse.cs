namespace qguardbackend.Data.DTOs
{
    public class GenericResponse
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public object Data { get; set; } = null;
    }

    public class Response<T> : GenericResponse
    {
        public new T Data { get; set; }
    }
}
