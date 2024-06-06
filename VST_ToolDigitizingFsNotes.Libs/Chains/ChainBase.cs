namespace VST_ToolDigitizingFsNotes.Libs.Chains
{
    public abstract class ChainBaseRequest<T> where T : class
    {
        public T? Result { get; internal set; } = null;
        public bool Handled { get; protected set; } = false;
        internal virtual void SetHandled(bool handled)
        {
            Handled = handled;
        }
    }

    public interface IHandleChain<T> where T : class
    {
        void Handle(T request);
        void SetNext(IHandleChain<T> nextChain);
    }

    public abstract class HandleChainBase<T> : IHandleChain<T> where T : class
    {
        protected IHandleChain<T>? _nextChain;
        public void SetNext(IHandleChain<T> nextChain)
        {
            _nextChain = nextChain;
        }
        public abstract void Handle(T request);
    }
}
