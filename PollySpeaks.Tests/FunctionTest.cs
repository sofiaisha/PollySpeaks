using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Amazon.Lambda.TestUtilities;
using Moq;
using Amazon.Polly;
using Amazon.S3;
using Amazon.Polly.Model;
using System.IO;
using System.Text;
using System;

namespace PollySpeaks.Tests
{
    public class FunctionTest
    {
        [Fact]
        public async Task TestToProcessGhost()
        {
            var mockPolly = new Mock<IAmazonPolly>();
            var mockS3 = new Mock<IAmazonS3>();
            var speechResponse = new SynthesizeSpeechResponse();
            speechResponse.AudioStream = new MemoryStream(Encoding.UTF8.GetBytes("whatever"));


            mockPolly.Setup(m => m.SynthesizeSpeechAsync(It.IsAny<SynthesizeSpeechRequest>(), It.IsAny<System.Threading.CancellationToken>()))
                  .ReturnsAsync(speechResponse);
            
            var expectedurl = "<h1 class='post-full-title'>Test Title</h1><div class='post-content'>Test Body</div>";

            var payload = new GhostPayload { text = expectedurl };

            var function = new Function(mockS3.Object, mockPolly.Object);
            var context = new TestLambdaContext();
            bool actual = await function.FunctionHandler(payload, context);

            Assert.True(actual);

        }

        [Fact(Skip = "This is an Integration test")]
        public async Task IntegrationTestToProcessGhost()
        {
            var expectedurl = "https://thebeebs.co.uk/being-cool-isnt-an-objective";
            var payload = new GhostPayload { text = expectedurl };

            var function = new Function();
            var context = new TestLambdaContext();
            bool actual = await function.FunctionHandler(payload, context);
            Assert.True(actual);
        }

        [Fact]
        public void GetTextFromURl()
        {
            var mockPolly = new Mock<IAmazonPolly>();
            var mockS3 = new Mock<IAmazonS3>();

            var Function = new Function(mockS3.Object, mockPolly.Object);

            var expectedurl = "<h1 class='post-full-title'>Test Title</h1><div class='post-content'>Test Body</div>";

            var actual = Function.GetTextFromWebsite(expectedurl);
         
            Assert.Contains("Test Title", actual.Item1);
            Assert.Contains("Test Body", actual.Item2);
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
        public async Task CallFucntionWithEmptyStringPayload()
        {
            var function = new Function();
            var context = new TestLambdaContext();
            var payload = new GhostPayload { text = string.Empty };

            await Assert.ThrowsAsync<ArgumentException>(() => function.FunctionHandler(payload, context));         
        }

        [Fact]
        public async Task CallFucntionWithEmptyNullPayload()
        {
            var function = new Function();
            var context = new TestLambdaContext();
            var payload = new GhostPayload { text = null };

            await Assert.ThrowsAsync<ArgumentException>(() => function.FunctionHandler(payload, context));
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
