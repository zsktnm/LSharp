namespace LSharp.Client.Common
{
    public class RequestResult
    {
        public List<string> Errors { get; } = new List<string>();

        public bool IsValid => Errors.Count == 0;

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

    }
}
