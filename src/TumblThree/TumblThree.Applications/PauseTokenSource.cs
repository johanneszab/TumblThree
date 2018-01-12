using System.Threading.Tasks;

namespace TumblThree.Applications
{
    public class PauseTokenSource
    {
        internal static readonly TaskCompletionSource<bool> s_completedTcs;
        private readonly object m_lockObject = new object();
	    private bool m_paused; // could use m_resumeRequest as flag too
        private TaskCompletionSource<bool> m_pauseResponse;
        private TaskCompletionSource<bool> m_resumeRequest;

        static PauseTokenSource()
        {
            s_completedTcs = new TaskCompletionSource<bool>();
            s_completedTcs.SetResult(true);
        }

        public bool IsPaused
        {
            get
            {
                lock (m_lockObject)
                {
	                return m_paused;
                }
            }
        }

        public PauseToken Token => new PauseToken(this);

	    public void Pause()
        {
            lock (m_lockObject)
            {
                if (m_paused)
                {
                    return;
                }
                m_paused = true;
                m_pauseResponse = s_completedTcs;
                m_resumeRequest = new TaskCompletionSource<bool>();
            }
        }

        public void Resume()
        {
            TaskCompletionSource<bool> resumeRequest;

            lock (m_lockObject)
            {
                if (!m_paused)
                {
                    return;
                }
                m_paused = false;
                resumeRequest = m_resumeRequest;
                m_resumeRequest = null;
            }

            resumeRequest.TrySetResult(true);
        }

        // pause with a feedback that
        // the producer task has reached the paused state

        public Task PauseWithResponseAsync()
        {
            Task responseTask;

            lock (m_lockObject)
            {
                if (m_paused)
                {
                    return m_pauseResponse.Task;
                }
                m_paused = true;
                m_pauseResponse = new TaskCompletionSource<bool>();
                m_resumeRequest = new TaskCompletionSource<bool>();
                responseTask = m_pauseResponse.Task;
            }

            return responseTask;
        }

        public Task WaitWhilePausedWithResponseAsyc()
        {
            Task resumeTask;
            TaskCompletionSource<bool> response;

            lock (m_lockObject)
            {
                if (!m_paused)
                {
                    return s_completedTcs.Task;
                }
                response = m_pauseResponse;
                resumeTask = m_resumeRequest.Task;
            }

            response.TrySetResult(true);
            return resumeTask;
        }
    }

    public struct PauseToken
    {
        private readonly PauseTokenSource m_source;

        public PauseToken(PauseTokenSource source)
        {
            m_source = source;
        }

        public bool IsPaused => (m_source != null) && m_source.IsPaused;

	    public Task WaitWhilePausedWithResponseAsyc()
        {
            return IsPaused
                ? m_source.WaitWhilePausedWithResponseAsyc()
                : PauseTokenSource.s_completedTcs.Task;
        }
    }
}
