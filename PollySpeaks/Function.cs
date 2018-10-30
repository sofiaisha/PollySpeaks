using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.Polly;
using Amazon.Polly.Model;
using Amazon.S3;
using Amazon.S3.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace PollySpeaks
{
    public class GhostPayload
    {
        public string text { get; set; }
        public bool unfurl_links { get; set; }
        public string icon_url { get; set; }
        public string username { get; set; }
    }

    public class Function
    {
        IAmazonS3 S3Client { get; set; }
        IAmazonPolly PollyClient { get; set; }

        private const string bucketName = "pollyspeaks";
        private static readonly RegionEndpoint awsRegion = RegionEndpoint.EUWest1;

        private readonly HtmlAgilityPack.HtmlWeb AglityPackWeb =  new HtmlAgilityPack.HtmlWeb();
    
        public Function()
        {
            S3Client = new AmazonS3Client(awsRegion);
            PollyClient = new AmazonPollyClient(awsRegion);
        }

        public Function(IAmazonS3 s3Client, IAmazonPolly pollyClient)
        {
            this.PollyClient = pollyClient;
            this.S3Client = s3Client;
        }

        /// <summary>
        /// A function that take a GhostPayload and proceeds to do stuff with it
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns>True or false depending in success</returns>
        public async Task<bool> FunctionHandler(GhostPayload input, ILambdaContext context)
        {
            if (string.IsNullOrEmpty(input.text))
            {
                return false;
            }

            input.text = input.text.TrimEnd("/".ToCharArray());
            var itemName = input.text.Substring(input.text.LastIndexOf('/'), input.text.Length- input.text.LastIndexOf('/')).TrimStart("/".ToCharArray());
            var text = GetTextFromWebsite(input.text);

            var speechList = SplitText(text.Item2, 1500);
            using (var output = new MemoryStream())
            {
                foreach (var speech in speechList)
                {
                    SynthesizeSpeechRequest sreq = new SynthesizeSpeechRequest
                    {
                        Text = $"The blog title is: {text.Item1}.  I will now read the blog: {speech}",
                        OutputFormat = OutputFormat.Mp3,
                        VoiceId = VoiceId.Brian
                    };

                    var pollyResponse = await PollyClient.SynthesizeSpeechAsync(sreq);

                    await pollyResponse.AudioStream.CopyToAsync(output);
                    output.Position = output.Length;
                }

                output.Position = 0;
                var putRequest1 = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = $"{itemName}.mp3",
                    InputStream = output

                };

                await S3Client.PutObjectAsync(putRequest1);
 
            }
            return true;
        }

        public (string, string) GetTextFromWebsite(string urlOrHtml)
        {
            HtmlAgilityPack.HtmlDocument doc;

            if (urlOrHtml.StartsWith("http")){
                doc = this.AglityPackWeb.Load(urlOrHtml);
            }
            else {
                doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(urlOrHtml);
            }

            var title = doc.DocumentNode.SelectNodes("//h1[@class='post-full-title']")[0].InnerText;
            var document = doc.DocumentNode.SelectSingleNode("//div[@class='post-content']").InnerText; 

            return (title, document);
        }

        public static List<string> SplitText(string concatstring, int v)
        {
            var list = new List<String>();

            while (concatstring.Length > 0)
            {
                if (concatstring.Length > v)
                {
                    var tempNumber = v;
                    // Find the last space.
                    while (concatstring.Substring(tempNumber - 2, 1) != " ")
                    {
                        tempNumber--;
                    }
                    
                    list.Add(concatstring.Substring(0, tempNumber - 1));                    
                    concatstring = concatstring.Substring(tempNumber - 1, (concatstring.Length - tempNumber));
                }
                else
                {
                    list.Add(concatstring);
                    concatstring = "";
                }
            }            

            return list;
        }
    }   

}
