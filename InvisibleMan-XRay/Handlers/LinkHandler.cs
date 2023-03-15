using System.Diagnostics;

namespace InvisibleManXRay.Handlers
{
    using Values;

    public class LinkHandler : Handler
    {
        public void OpenGitHubRepositoryLink()
        {
            Process.Start(new ProcessStartInfo(Route.REPOSITORY) {
                UseShellExecute = true
            });
        }

        public void OpenBugReportingLink()
        {
            Process.Start(new ProcessStartInfo(Route.ISSUES) {
                UseShellExecute = true
            });
        }

        public void OpenLatestReleaseLink()
        {
            Process.Start(new ProcessStartInfo(Route.LATEST_RELEASE) {
                UseShellExecute = true
            });
        }
    }
}