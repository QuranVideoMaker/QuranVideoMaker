using QuranVideoMaker.Utilities;

namespace QuranVideoMaker.Tests.Utilities
{
    public class UpdateCheckerTests
    {
        [Fact]
        public async Task CheckForUpdates()
        {
            // Arrange

            // Act
            var version = await UpdateChecker.CheckForUpdates();

            // Assert
            Assert.True(version.LatestVersion > new Version(0, 0, 1));
        }
    }
}
