﻿using NUnit.Framework;
using Utils;

namespace FrameworkTests.Utils
{
    [TestFixture]
    public class Base58Tests
    {
        private readonly List<Base58Sample> samples = new List<Base58Sample>
        {
            new Base58Sample(bytes: [0x3B, 0xD2, 0x55, 0xFD, 0x71], encoded: "7kTCFpU"),
            new Base58Sample(bytes: [0x13, 0x97, 0x32, 0x86, 0xD3, 0xAF], encoded: "AkpgeMJn"),
            new Base58Sample(bytes: [0x7D, 0x0B, 0xF5, 0x26, 0x3D, 0x6F, 0xA7], encoded: "5jr2A43VXC"),
            new Base58Sample(bytes: [0x36, 0x70, 0xE2, 0x2C, 0x39, 0x1B, 0x2F, 0x84], encoded: "A79SCTbiauH"),
            new Base58Sample(bytes: [0x8C, 0xE8, 0xE9, 0x02, 0x3B, 0x7F, 0xC3, 0x10, 0x1F], encoded: "2o2fR77MzfQnr"),
            new Base58Sample(bytes: [0xDA, 0x8B, 0x1F, 0xC7, 0xE6, 0x22, 0xDE, 0x42, 0xC8, 0xEB], encoded: "DH8kviqidDGjmt"),
            new Base58Sample(bytes: [0x32, 0x61, 0x2A, 0x8D, 0x19, 0x30, 0xAA, 0x91, 0x4F, 0x94, 0xA2], encoded: "DVb12tvahZEhPJZ"),
            new Base58Sample(bytes: [0x59, 0xDC, 0xD5, 0x1F, 0x37, 0x8C, 0x2A, 0xB1, 0x0F, 0x30, 0xF8, 0x68], encoded: "2hMd33HzFnr2qFQ3h"),
            new Base58Sample(bytes: [0x65, 0x11, 0x29, 0xA4, 0x00, 0xAA, 0xB1, 0xD5, 0x5E, 0x0E, 0xE4, 0xF2, 0xE0], encoded: "9RFSB5gKLudsnYUYD5"),
            new Base58Sample(bytes: [0xDA, 0x83, 0x65, 0x66, 0xE3, 0x59, 0xB9, 0xF9, 0x17, 0xFC, 0x30, 0x09, 0xD3, 0xA5], encoded: "2PLJtoBLFsMF6FZApuZn"),
            new Base58Sample(bytes: [0x87, 0x64, 0x7E, 0x2D, 0x37, 0x6C, 0x6E, 0x43, 0xE1, 0xA0, 0x62, 0x7B, 0xF3, 0x96, 0x72], encoded: "4nhNnC59yxcV8QyUoCDSZ"),
            new Base58Sample(bytes: [0xCE, 0x5F, 0x33, 0xC2, 0xD9, 0x5C, 0xAA, 0x37, 0xDE, 0x57, 0x97, 0xF9, 0x06, 0x8B, 0xA2, 0xA5], encoded: "SV46mhqPMSgTs5uq1FDfek"),
            new Base58Sample(bytes: [0x29, 0x7B, 0x36, 0x7F, 0xE9, 0x71, 0x64, 0xDA, 0xE4, 0xDA, 0x51, 0x25, 0x5A, 0x8A, 0xCE, 0x00, 0xE0], encoded: "PcJjJn8vvbZz6gbqW6LFMUs"),
            new Base58Sample(bytes: [0x8F, 0x8C, 0x7C, 0x45, 0x18, 0xB2, 0xF0, 0xCE, 0x6C, 0xD2, 0x5A, 0x0C, 0xFA, 0x92, 0x27, 0xDB, 0x6E, 0xA2], encoded: "6xLB547pAFkXer4Q8W4fwFbUm"),
            new Base58Sample(bytes: [0xD9, 0x01, 0x38, 0x2F, 0xF2, 0x2E, 0x9C, 0x44, 0x0C, 0xC0, 0x7A, 0x6A, 0x6E, 0xDF, 0x3B, 0x18, 0xD3, 0x13, 0xF0], encoded: "gjBZZt15fbfHipFk8gd7dGxt23"),
            new Base58Sample(bytes: [0xA2, 0x4E, 0x95, 0x84, 0x08, 0x26, 0x1A, 0x82, 0xC2, 0x24, 0x2E, 0xD9, 0xEC, 0x16, 0x7B, 0xF8, 0x55, 0x16, 0x11, 0x9B], encoded: "3G9hzEeVm1jiK6m3j8b7j8YtNZLv"),
            new Base58Sample(bytes: [0x9F, 0xA3, 0xAF, 0x20, 0xD1, 0x77, 0x2C, 0x03, 0x21, 0x16, 0x70, 0x77, 0x16, 0x1A, 0x08, 0x9C, 0xBD, 0xEB, 0x98, 0x51, 0x2B], encoded: "ApMczxbJwYX6WoAkVFULC6K2Sjwpn"),
            new Base58Sample(bytes: [0xDB, 0x71, 0xEE, 0xDB, 0xBB, 0x1A, 0x05, 0xDE, 0xA2, 0x8F, 0x45, 0xD5, 0xDF, 0xC4, 0x29, 0xF0, 0xED, 0x59, 0x99, 0x69, 0x5E, 0xF4], encoded: "22ZUBBs5677oBH8Rk6LdQUdmERoddrB"),
            new Base58Sample(bytes: [0x8D, 0x45, 0x2A, 0x0D, 0xDC, 0xDE, 0x81, 0x66, 0x38, 0x3E, 0xA1, 0x53, 0x78, 0xDB, 0x7A, 0x77, 0x96, 0xAF, 0xD3, 0xC7, 0x48, 0xDF, 0x5B], encoded: "3vEdB7WnrDtKb59Y2aiyRzkses6PSnRp"),
            new Base58Sample(bytes: [0x48, 0xB8, 0x90, 0x04, 0xFE, 0x20, 0xD6, 0xF0, 0x59, 0x42, 0x54, 0x7D, 0x98, 0xFC, 0x18, 0xEF, 0x1D, 0xDF, 0xAD, 0xDE, 0x02, 0x7A, 0xE9, 0xB9], encoded: "7dWkSmx91USJCM4BnZXQPqaNomF8hn54L"),
            new Base58Sample(bytes: [0xED, 0xE9, 0x9E, 0x72, 0xA8, 0x49, 0x6A, 0xDF, 0x37, 0xBD, 0xD1, 0x9D, 0x63, 0x8D, 0xD6, 0x2C, 0x69, 0x73, 0xAB, 0xA1, 0xB4, 0x8F, 0x9C, 0x24, 0xB7], encoded: "2ejQDcaqgo83YbshzYRQYLPfc1AJC2AEtqt"),
            new Base58Sample(bytes: [0x4D, 0x60, 0x50, 0x78, 0x12, 0x1E, 0xAA, 0x3A, 0xFD, 0x18, 0x1C, 0x0F, 0x4A, 0x8B, 0x28, 0x7E, 0x76, 0x4E, 0x76, 0xFB, 0x27, 0xE9, 0xDB, 0xC4, 0x75, 0x7C], encoded: "3NRTALTuo84ypKwJFEqQEaEvvjtHrRJ1r23V"),
            new Base58Sample(bytes: [0xE0, 0x47, 0x47, 0xF9, 0xAA, 0x9D, 0x1C, 0x0B, 0x52, 0x4E, 0x99, 0xFA, 0xFC, 0x0A, 0xDA, 0xE7, 0x3C, 0x8B, 0xBC, 0xA6, 0x43, 0x69, 0x7D, 0xBD, 0x79, 0xB8, 0xF7], encoded: "XK7a4uhcz27RqpC3bF6zm6Kocwv3aiznakNdt"),
            new Base58Sample(bytes: [0xBE, 0xB5, 0x99, 0x5C, 0x58, 0xDD, 0xDA, 0xBA, 0x91, 0xDE, 0xBD, 0xCF, 0x38, 0x59, 0xD6, 0x07, 0x62, 0xC7, 0x6B, 0x05, 0x0A, 0xA4, 0x45, 0x2C, 0x93, 0xD1, 0x51, 0xF7], encoded: "2xmULLG4v35DY5oLoz9BsaVoz7SN7TW6bLyuzLe"),
            new Base58Sample(bytes: [0xBE, 0x42, 0xF4, 0xB5, 0x5F, 0xC4, 0x14, 0x32, 0xC8, 0x48, 0x44, 0x16, 0x8E, 0xEC, 0x9D, 0x89, 0xA8, 0xD7, 0xC8, 0xF0, 0xCD, 0x7A, 0x27, 0x81, 0xF0, 0x46, 0xD2, 0xC5, 0x3A], encoded: "9dxu2xHiMWGYdxce6DsAzQQHZ2PPEAAeNvLorjPo"),
            new Base58Sample(bytes: [0x62, 0x43, 0x36, 0xBD, 0x70, 0x26, 0x33, 0xDE, 0x83, 0x6B, 0xF5, 0x91, 0x26, 0x22, 0x82, 0x22, 0xD8, 0x94, 0x18, 0xE5, 0x4D, 0x1C, 0x69, 0x99, 0x45, 0x60, 0xAC, 0xD8, 0xCB, 0xF1], encoded: "LgyMNWbEQmwxKmQH2aK3RdKbT3yqLEgj6tWPxJFFv"),
            new Base58Sample(bytes: [0x2A, 0xE7, 0x52, 0x36, 0xDD, 0x7F, 0x8D, 0x3A, 0x3C, 0x30, 0x13, 0x61, 0xDA, 0x60, 0x22, 0x2D, 0x33, 0x4E, 0x3E, 0xD6, 0x88, 0xEB, 0xE2, 0xBB, 0xC2, 0x6E, 0xDE, 0x35, 0xE9, 0xDC, 0xC4], encoded: "ewm9uMxpDwM2nNiU1AWtEhGJgmnHBAsSd1wSa7oKSb"),
            new Base58Sample(bytes: [0x0F, 0xC6, 0x8A, 0x6D, 0xB5, 0x66, 0x27, 0xDE, 0x29, 0x22, 0x6D, 0xCC, 0xCA, 0xD8, 0x3B, 0x41, 0x63, 0x6B, 0x09, 0xE1, 0xB0, 0xA1, 0x33, 0x64, 0x58, 0xAB, 0x32, 0x75, 0xB9, 0xE7, 0xBA, 0xC6], encoded: "24ahweceSmfNAruMDjBcHmnsyH1bGBNJ4of8kzeUcamb"),
            new Base58Sample(bytes: [0xF1, 0x57, 0xA5, 0xA5, 0x8D, 0x69, 0xD6, 0xD8, 0xFA, 0x80, 0x20, 0x1F, 0x67, 0x33, 0x6A, 0xF8, 0x5A, 0xE3, 0x4F, 0xB5, 0x9E, 0xAD, 0x90, 0x87, 0x68, 0x5B, 0xD6, 0x6B, 0x03, 0x28, 0x5E, 0xBE, 0xBD], encoded: "2EhEUHo4bppZ7AV7fwmRMh4VyxHEJ8vpZPKFsnmmLAzXYg"),
            new Base58Sample(bytes: [0x23, 0xC2, 0x79, 0xFD, 0xF2, 0x5F, 0xB2, 0x60, 0x80, 0xDA, 0x76, 0xA3, 0x10, 0x03, 0x4F, 0x00, 0xD3, 0x9B, 0xD6, 0x9D, 0xE0, 0xA3, 0xFC, 0x21, 0x16, 0x9B, 0x3A, 0x9A, 0x3E, 0x78, 0x9E, 0x05, 0x8C, 0x3C], encoded: "otSxbeDfuisL3V3azVS1mYrdDwZC4BYUddFo3TkmiJd3Es"),
            new Base58Sample(bytes: [0x63, 0xFF, 0x58, 0xE7, 0x24, 0xC6, 0x50, 0x8B, 0xE5, 0x58, 0x11, 0xCE, 0xC8, 0xD3, 0x2D, 0xDE, 0xD5, 0x20, 0x8D, 0x76, 0x76, 0xD7, 0xCE, 0x82, 0x76, 0xBA, 0x65, 0xC5, 0xE3, 0x82, 0xCF, 0x86, 0xAB, 0x07, 0x21], encoded: "Ayi7DCDkkiEUtsfZh5n9am5zKJ2B5y2WoBgEpYhjKMDbg2Ug")
        };

        [Test]
        public void EncodeDecode()
        {
            foreach (var sample in samples)
            {
                Test(sample);
            }
        }

        private void Test(Base58Sample sample)
        {
            var encoded = Base58.Encode(sample.Bytes);
            var decoded = Base58.Decode(encoded);

            Assert.That(encoded, Is.EqualTo(sample.Encoded));
            Assert.That(decoded, Is.EqualTo(sample.Bytes));
        }

        public class Base58Sample
        {
            public Base58Sample(byte[] bytes, string encoded)
            {
                Bytes = bytes;
                Encoded = encoded;
            }

            public byte[] Bytes { get; }
            public string Encoded { get; }
        }
    }
}
