﻿namespace Unosquare.Labs.EmbedIO.Tests
{
    using Newtonsoft.Json;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using Unosquare.Labs.EmbedIO.Tests.Properties;
    using Unosquare.Labs.EmbedIO.Modules;
    using Unosquare.Labs.EmbedIO.Tests.TestObjects;

    [TestFixture]
    public class WebApiModuleTest
    {
        protected WebServer WebServer;
        protected TestConsoleLog Logger = new TestConsoleLog();

        [SetUp]
        public void Init()
        {
            WebServer = new WebServer(Resources.ServerAddress, Logger).WithWebApiController<TestController>();
            WebServer.RunAsync();
        }

        [Test]
        public void TestWebApi()
        {
            Assert.IsNotNull(WebServer.Module<WebApiModule>(), "WebServer has WebApiModule");

            Assert.AreEqual(WebServer.Module<WebApiModule>().ControllersCount, 1, "WebApiModule has one controller");
        }
        
        [Test]
        public void GetJsonData()
        {
            List<Person> remoteList;

            var request = (HttpWebRequest) WebRequest.Create(Resources.ServerAddress + TestController.GetPath);

            using (var response = (HttpWebResponse) request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var jsonBody = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Assert.IsNotNullOrEmpty(jsonBody, "Json Body is not null or empty");

                remoteList = JsonConvert.DeserializeObject<List<Person>>(jsonBody);

                Assert.IsNotNull(remoteList, "Json Object is not null");
                Assert.AreEqual(remoteList.Count, PeopleRepository.Database.Count, "Remote list count equals local list");
            }

            TestHelper.ValidatePerson(Resources.ServerAddress + TestController.GetPath + remoteList.First().Key);
        }

        [Test]
        public void GetJsonDataWithMiddleUrl()
        {
            var person = PeopleRepository.Database.First();
            TestHelper.ValidatePerson(Resources.ServerAddress + TestController.GetMiddlePath.Replace("*", person.Key.ToString()));
        }

        [Test]
        public void GetJsonAsyncData()
        {
            var person = PeopleRepository.Database.First();
            TestHelper.ValidatePerson(Resources.ServerAddress + TestController.GetAsyncPath + person.Key);
        }

        [Test]
        public void PostJsonData()
        {
            var model = new Person() {Key = 10, Name = "Test"};
            var request = (HttpWebRequest) WebRequest.Create(Resources.ServerAddress + TestController.GetPath);
            request.Method = "POST";

            using (var dataStream = request.GetRequestStream())
            {
                var byteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model));
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            using (var response = (HttpWebResponse) request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var jsonString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                Assert.IsNotNullOrEmpty(jsonString);

                var json = JsonConvert.DeserializeObject<Person>(jsonString);
                Assert.IsNotNull(json);
                Assert.AreEqual(json.Name, model.Name);
            }
        }

        [Test]
        public void TestWebApiWithConstructor()
        {
            const string name = "Test";

            WebServer.Module<WebApiModule>().RegisterController(() => new TestControllerWithConstructor(name));

            var request = (HttpWebRequest) WebRequest.Create(Resources.ServerAddress + "name");

            using (var response = (HttpWebResponse) request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var body = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Assert.AreEqual(body, name);
            }
        }

        [TearDown]
        public void Kill()
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
            WebServer.Dispose();
        }
    }
}