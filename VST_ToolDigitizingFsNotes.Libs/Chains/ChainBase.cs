namespace VST_ToolDigitizingFsNotes.Libs.Chains
{
    public abstract class ChainBaseRequest<TResult> where TResult : class
    {
        public TResult? Result { get; internal set; } = null;
        public bool Handled { get; protected set; } = false;
        internal virtual void SetHandled(bool handled)
        {
            Handled = handled;
        }
    }

    public interface IHandleChain<TRequest> where TRequest : class
    {
        void Handle(TRequest request);
        void SetNext(IHandleChain<TRequest> nextChain);
    }

    public abstract class HandleChainBase<TRequest> : IHandleChain<TRequest> where TRequest : class
    {
        protected IHandleChain<TRequest>? _nextChain;
        public void SetNext(IHandleChain<TRequest> nextChain)
        {
            _nextChain = nextChain;
        }
        public abstract void Handle(TRequest request);
    }
}
