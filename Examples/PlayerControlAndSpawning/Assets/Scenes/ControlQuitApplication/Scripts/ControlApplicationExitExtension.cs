using System.Collections;
using UnityEngine;

/// <summary>
/// Draws the runtime UI for the Control Application Exit example.
/// </summary>
public class ControlApplicationExitExtension : BaseMonoExtension
{
    [Tooltip("When CountDownBeforeExit is enable, it will delay the application exit before allowing the application to exit.")]
    public bool CountDownBeforeExit = true;

    [Tooltip("When CountDownBeforeExit is enable, this determines how long it will wait before exiting.")]
    [Range(1, 10)]
    public int CountDownPeriod = 5;
    private bool m_ExitPending;
    private bool m_CanExitApplication;

    protected override void OnInitialize()
    {
        // Start off by setting the additional check to false
        m_ExtendedNetworkManager.CanQuitApplication = false;
        m_CanExitApplication = m_ExitPending = false;
        m_ExtendedNetworkManager.CanApplicationQuit = CanApplicationQuit;
        base.OnInitialize();
    }

    /// <summary>
    /// Sets the <see cref="m_ExitPending"/> to true and notifes the <see cref="ExtendedNetworkManager"/> that the
    /// application is exiting (so it can, in turn, notify all of the extensions running).
    /// </summary>
    private void SetExitPending()
    {
        if (!m_ExitPending)
        {
            m_ExitPending = true;
            m_ExtendedNetworkManager.ApplicationExitInProgress();
        }
    }

    private bool CanApplicationQuit()
    {
        // If we are in a network session, then we need to wait until the NetworkManager is shutdown
        // before we exit.
        if (m_ExtendedNetworkManager.IsListening && !m_ExtendedNetworkManager.ShutdownInProgress)
        {
            SetExitPending();
            m_ExtendedNetworkManager.OnClientStopped += OnClientStopped;
            // Begin the shutdown process.
            m_ExtendedNetworkManager.Shutdown();
            // Block exiting the application
            return false;
        }
        else if (m_ExtendedNetworkManager.ShutdownInProgress)
        {
            // We are still shutting down, block exiting the application
            return false;
        }
        else // If we have the count down set and we are not in a network session, then block exiting the application.
        if (CountDownBeforeExit && !m_ExitPending && !m_CanExitApplication)
        {
            SetExitPending();
            StartCoroutine(DelayShutdownForVerification());
            return false;
        }
        else // If we do not have the count down set and we are not in a network session, then allow the exit to happen.
        if (!CountDownBeforeExit && !m_ExitPending && !m_CanExitApplication)
        {
            m_CanExitApplication = true;
        }
        else if (m_ExitPending)
        {
            return false;
        }
        return m_CanExitApplication;
    }

    private void OnClientStopped(bool obj)
    {
        m_ExtendedNetworkManager.OnClientStopped -= OnClientStopped;
        if (m_ExitPending)
        {
            if (CountDownBeforeExit)
            {
                // If we have a count down enabled, then start the delay coroutine.
                StartCoroutine(DelayShutdownForVerification());
            }
            else
            {
                // If we have no count down enabled, then just exit once the
                // NetworkManager has ended the session.
                m_ExitPending = false;
                m_CanExitApplication = true;
                m_ExtendedNetworkManager.QuitApplication();
            }
        }
        else
        {
            Debug.LogError($"[{nameof(ControlApplicationExitExtension)}] OnClientStopped was invoked without performing any application exit action!");
        }
    }

    /// <summary>
    /// Delays the application exit.
    /// </summary>
    /// <remarks>
    /// This is just to show you can override the default application exit behavior
    /// and make the application wait until you are ready for it to exit.
    /// </remarks>
    private IEnumerator DelayShutdownForVerification()
    {
        var waitForOneSecond = new WaitForSeconds(1.0f);
        var timeToQuit = CountDownPeriod;
        while (timeToQuit > 0)
        {
            m_ExtendedNetworkManager.LogMessage($"Application exiting in {timeToQuit}...");
            yield return waitForOneSecond;
            timeToQuit--;
        }
        m_ExitPending = false;
        m_CanExitApplication = true;
        m_ExtendedNetworkManager.QuitApplication();
    }

    private Rect TopRightGUI(Rect totalRectSize)
    {
        var retButtonValues = Draw.Button(totalRectSize, "Quit");
        if (retButtonValues.Item2 && m_ExtendedNetworkManager.CanQuitApplication)
        {
            m_ExtendedNetworkManager.QuitApplication();
            return retButtonValues.Item1;
        }
        totalRectSize = retButtonValues.Item1;
        var retToggleValues = Draw.Toggle(totalRectSize, m_ExtendedNetworkManager.CanQuitApplication, "Can Quit");
        m_ExtendedNetworkManager.CanQuitApplication = retToggleValues.Item2;
        totalRectSize = retToggleValues.Item1;
        return totalRectSize;
    }

    protected override Rect OnGUIUpdate(Rect totalRectSize, ScreenSpaceRegions screenSpaceRegion)
    {
        switch (screenSpaceRegion)
        {
            case ScreenSpaceRegions.TopRight:
                {
                    totalRectSize = TopRightGUI(totalRectSize);
                    break;
                }
        }
        return base.OnGUIUpdate(totalRectSize, screenSpaceRegion);
    }
}
