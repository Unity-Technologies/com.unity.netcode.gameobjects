
namespace Unity.Netcode.TestHelpers.Runtime
{
    /// <summary>
    /// Derive from this class to create your own conditional handling for your <see cref="NetcodeIntegrationTest"/>
    /// integration tests when dealing with more complicated scenarios where initializing values, storing state to be
    /// used across several integration tests.
    /// </summary>
    public class ConditionalPredicateBase : IConditionalPredicate
    {
        private bool m_TimedOut;

        public bool TimedOut { get { return m_TimedOut; } }

        protected virtual bool OnHasConditionBeenReached()
        {
            return true;
        }

        public bool HasConditionBeenReached()
        {
            return OnHasConditionBeenReached();
        }

        protected virtual void OnStarted() { }

        public void Started()
        {
            OnStarted();
        }

        protected virtual void OnFinished() { }

        public void Finished(bool timedOut)
        {
            m_TimedOut = timedOut;
            OnFinished();
        }
    }

    public interface IConditionalPredicate
    {
        /// <summary>
        /// Test the conditions of the test to be reached
        /// </summary>
        bool HasConditionBeenReached();

        /// <summary>
        /// Wait for condition has started
        /// </summary>
        void Started();

        /// <summary>
        /// Wait for condition has finished:
        /// Condition(s) met or timed out
        /// </summary>
        void Finished(bool timedOut);

    }
}
