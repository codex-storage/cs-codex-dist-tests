using CodexTests;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace CodexReleaseTests.DataTests
{
    [TestFixture]
    public class LocalFilesTest : CodexDistTest
    {
        [Test]
        public void ShouldShowLocalFiles()
        {
            var node = StartCodex();

            var size1 = 123.KB();
            var size2 = 23.MB();
            var file1 = GenerateTestFile(size1);
            var file2 = GenerateTestFile(size2);

            var cid1 = node.UploadFile(file1);
            var cid2 = node.UploadFile(file2);

            var localFiles = node.LocalFiles();

            Assert.That(localFiles.Content.Length, Is.EqualTo(2));

            var local1 = localFiles.Content.Single(f => f.Cid == cid1);
            var local2 = localFiles.Content.Single(f => f.Cid == cid2);

            Assert.That(local1.Manifest.Protected, Is.False);
            Assert.That(local1.Manifest.OriginalBytes, Is.EqualTo(size1));
            Assert.That(local2.Manifest.Protected, Is.False);
            Assert.That(local2.Manifest.OriginalBytes, Is.EqualTo(size2));
        }
    }
}
