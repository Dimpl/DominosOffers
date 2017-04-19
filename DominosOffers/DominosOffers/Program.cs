using System;
using System.Data.SqlTypes;
using System.IO;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace DominosOffers
{
    public class Program
    {
        private const string Store = "98421";

        private const string Content = "{\n" +
                                       "\t  \"StoreNumber\": " + Store + ",\n" +
                                       "\t  \"DeviceId\": \"\",\n" +
                                       "\t  \"RequestKey\": {\n" +
                                       "\t  \t  \"Country\": \"AU\",\n" +
                                       "\t  \t  \"Application\": \"-\",\n" +
                                       "\t  \t  \"Version\": \"-\",\n" +
                                       "\t  \t  \"Culture\": \"en\"\n" +
                                       "\t  }\n" +
                                       "}";

        private const string Uri = "https://services.dominos.com.au/OffersApp/Vouchers/vouchers";

        public static void Main(string[] args)
        {
            try {
                var json = MakePostWebRequest(Uri, Content);
                PrintVoucherData(json);
                Exit();
            } catch (Exception e) {
                Console.Error.WriteLine("\nUnhandled Exception: " + e.ToString());
                Exit();
                Environment.Exit(1);
            }
        }

        public static string MakePostWebRequest(string uri, string postData)
        {
            // create a request
            var request = (HttpWebRequest) WebRequest.Create(uri);
            request.KeepAlive = false;
            request.ProtocolVersion = HttpVersion.Version11;
            request.Method = "POST";

            // turn our request string into a byte stream
            var postBytes = Encoding.ASCII.GetBytes(postData);

            // this is important - make sure you specify type this way
            request.ContentType = "application/json";
            request.ContentLength = postBytes.Length;

            // certificate hack from http://www.terminally-incoherent.com/blog/2008/05/05/send-a-https-post-request-with-c/
            System.Net.ServicePointManager.CertificatePolicy = new MyPolicy();

            using (var requestStream = request.GetRequestStream()) {
                requestStream.Write(postBytes, 0, postBytes.Length);
            }

            var response = (HttpWebResponse) request.GetResponse();
            return new StreamReader(response.GetResponseStream()).ReadToEnd();
        }

        public static void PrintVoucherData(string json)
        {
            using (var sReader = new StringReader(json))
            using (var reader = new JsonTextReader(sReader)) {
                var printString = false;
                var voucherCode = "";
                var product = "";
                var price = "";

                while (reader.Read()) {
                    if (reader.Path.StartsWith("Vouchers")) {
                        switch (reader.TokenType) {
                            case JsonToken.String:
                                var cut = reader.Path.LastIndexOf(".");
                                var token = (cut != -1) ? reader.Path.Substring(cut + 1) : "";
                                switch (token) {
                                    case "ServiceMethod":
                                        printString = (string) reader.Value == "Pickup";
                                        break;

                                    case "VoucherCode":
                                        voucherCode = (string) reader.Value;
                                        break;

                                    case "Description":
                                        var description = (string) reader.Value;
                                        var cut2 = description.IndexOf("$");
                                        if (cut2 == -1) {
                                            product = description;
                                            price = "";
                                            break;
                                        }
                                        product = description.Substring(0, description.LastIndexOf(" ", cut2 - 2));
                                        price = description.Substring(cut2, description.IndexOf(" ", cut2) - cut2);
                                        break;
                                }
                                break;

                            case JsonToken.EndObject:
                                if (printString) {
                                    Console.WriteLine("{0}: {1}{2}{3}", voucherCode, product, (price != "") ? ", " : "", price);
                                    printString = false;
                                }
                                break;
                        }
                    }
                }
            }
        }

        public static void Exit()
        {
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey(true);
        }
    }
}