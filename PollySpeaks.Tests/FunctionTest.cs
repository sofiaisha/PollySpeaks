using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using PollySpeaks;
using Newtonsoft.Json;
using Moq;
using Amazon.Polly;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Polly.Model;
using Amazon;

namespace PollySpeaks.Tests
{
    public class FunctionTest
    {
        [Fact]
        public async void TestToProcessGhost()
        {
            var mockPolly = new Mock<IAmazonPolly>();
            var mockS3 = new Mock<IAmazonS3>();
            var mockHtmlWeb = new Mock<HtmlAgilityPack.HtmlWeb>();

            mockPolly.Setup(m => m.SynthesizeSpeechAsync(It.IsAny<SynthesizeSpeechRequest>(), It.IsAny<System.Threading.CancellationToken>()))
                  .ReturnsAsync(new SynthesizeSpeechResponse());

            var expectedurl = "https://thebeebs.co.uk/speaking/";
            var payload = new GhostPayload { text = expectedurl };

            var function = new Function(mockS3.Object, mockPolly.Object, mockHtmlWeb.Object);
            var context = new TestLambdaContext();
            string actual = await function.FunctionHandler(payload, context);

        }

        [Fact]
        public async void IntegrationTestToProcessGhost()
        {
            var bucketRegion = RegionEndpoint.EUWest1;
            var S3Client = new AmazonS3Client(bucketRegion);
            
            var mockHtmlWeb = new Mock<HtmlAgilityPack.HtmlWeb>();

            Amazon.RegionEndpoint AWSRegion = Amazon.RegionEndpoint.EUWest1;
            var PollyClient = new AmazonPollyClient(AWSRegion);

            var expectedurl = "https://thebeebs.co.uk/being-cool-isnt-an-objective";
            var payload = new GhostPayload { text = expectedurl };

            var function = new Function();
            var context = new TestLambdaContext();
            string actual = await function.FunctionHandler(payload, context);

        }

        [Fact]
        public void GetTextFromURl()
        {
            var mockPolly = new Mock<IAmazonPolly>();
            var mockS3 = new Mock<IAmazonS3>();
            var mockHtmlWeb = new Mock<HtmlAgilityPack.HtmlWeb>();
            var Function = new Function(mockS3.Object, mockPolly.Object, mockHtmlWeb.Object);

            var url = "https://thebeebs.co.uk/speaking/";

            var actual = Function.GetTextFromWebsite(url);
         
            Assert.Contains("Speaking", actual.Item1);
            Assert.Contains("happy to speak at pretty", actual.Item2);
        }

        [Fact]
        public void SplitTextInto1500()
        {
            string text = "hello ";
            string concatstring = string.Empty;
            for (int i = 0; i < 1500; i++)
            {
                concatstring = concatstring + text;
            }

            List<string> actual = Function.SplitText(concatstring, 1500);
            
            Assert.Equal(7, actual.Count);
            Assert.EndsWith("hello ", actual.Last());
            Assert.EndsWith("hello ", actual.First());
        }

        [Fact]
        public void SendSplitFunctionShortText()
        {
            string text = "hello ";
            
            List<string> actual = Function.SplitText(text, 1500);

            Assert.Single(actual);
        }

    }
}
