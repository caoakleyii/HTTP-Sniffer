namespace HttpLogger.Models
{
    public class ThreadObject
    {
        public delegate object Callback (object response);

        public ThreadObject()
        {
            this.ThreadCallback = delegate (object o) { return o;  };
        }
        public object ThreadStartObject { get; set; }

        public Callback ThreadCallback { get; set; }
    }
}
