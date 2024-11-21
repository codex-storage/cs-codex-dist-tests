using CodexTests;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace CodexReleaseTests.DataTests
{
    public class ThreeClientTest : AutoBootstrapDistTest
    {
        [Test]
        public void ThreeClient()
        {
            var primary = StartCodex();
            var secondary = StartCodex();

            var testFile = GenerateTestFile(10.MB());

            var contentId = primary.UploadFile(testFile);
            AssertNodesContainFile(contentId, primary);

            var downloadedFile = secondary.DownloadContent(contentId);
            AssertNodesContainFile(contentId, primary, secondary);

            testFile.AssertIsEqual(downloadedFile);
        }
    }
}
