
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

        /// <summary>
        /// Will be set to <see cref="true"/> if timed out.
        /// </summary>
        public bool TimedOut { get { return m_TimedOut; } }

        /// <summary>
        /// Override this method to incorporate your own conditional logic
        /// </summary>
        /// <returns>true for condition being met and false for the condition has yet to be met.</returns>
        protected virtual bool OnHasConditionBeenReached()
        {
            return true;
        }

        /// <inheritdoc/>
        public bool HasConditionBeenReached()
        {
            return OnHasConditionBeenReached();
        }

        /// <summary>
        /// Override this to initialize anything for the conditioinal check.
        /// </summary>
        protected virtual void OnStarted() { }

        /// <inheritdoc/>
        public void Started()
        {
            OnStarted();
        }

        /// <summary>
        /// Override this to clean up anything used in the conditional check.
        /// </summary>
        protected virtual void OnFinished() { }

        /// <inheritdoc/>
        public void Finished(bool timedOut)
        {
            m_TimedOut = timedOut;
            OnFinished();
        }
    }

    /// <summary>
    /// A conditional predicate interface used with integration testing.
    /// </summary>
    public interface IConditionalPredicate
    {
        /// <summary>
        /// Test the conditions of the test to be reached
        /// </summary>
        /// <returns><see cref="true"/> or <see cref="false"/></returns>
        bool HasConditionBeenReached();

        /// <summary>
        /// Wait for condition has started
        /// </summary>
        void Started();

        /// <summary>
        /// Wait for condition has finished: <br />
        /// Condition(s) met or timed out
        /// </summary>
        /// <param name="timedOut"><see cref="true"/> or <see cref="false"/></param>
        void Finished(bool timedOut);

    }
}
