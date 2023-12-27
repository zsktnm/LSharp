namespace LSharp.Client.Common
{
    public class RequestResult
    {
        public List<string> Errors { get; } = new List<string>();

        public bool IsValid => Errors.Count == 0;

        private bool hasAccess = true;

        public bool HasAccess => hasAccess;

        private RequestResult() { }

        private RequestResult(string error)
        {
            Errors.Add(error);
        }

        private RequestResult(IEnumerable<string> errors)
        {
            Errors.AddRange(errors);
        }

        public static RequestResult FromErrors(IEnumerable<string> errors)
        {
            return new RequestResult(errors);
        }

        public static RequestResult FromError(string error)
        {
            return new RequestResult(error);
        }


        public static RequestResult FromSuccess()
        {
            return new RequestResult();
        }

        public static RequestResult NoAccess()
        {
            var result = new RequestResult();
            result.hasAccess = false;
            return result;
        }

    }

    public class RequestResult<T>
    {
        public List<string> Errors { get; } = new List<string>();

        public bool IsValid => Errors.Count == 0;

        private bool hasAccess = true;

        public bool HasAccess => hasAccess;

        private T? value = default;

        public T? Value => value;

        private RequestResult() { }

        private RequestResult(T value) 
        {
            this.value = value;
        }

        private RequestResult(string error)
        {
            Errors.Add(error);
        }

        private RequestResult(IEnumerable<string> errors)
        {
            Errors.AddRange(errors);
        }

        public static RequestResult<T> FromErrors(IEnumerable<string> errors)
        {
            return new RequestResult<T>(errors);
        }

        public static RequestResult<T> FromError(string error)
        {
            return new RequestResult<T>(error);
        }


        public static RequestResult<T> FromSuccess(T value)
        {
            return new RequestResult<T>(value);
        }

        public static RequestResult<T> NoAccess()
        {
            var result = new RequestResult<T>();
            result.hasAccess = false;
            return result;
        }

    }
}
