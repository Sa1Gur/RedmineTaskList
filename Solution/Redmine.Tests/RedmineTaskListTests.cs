using System;
using System.IO;
using System.Net;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;

namespace Redmine.Tests
{
    [TestFixture]
    public class RedmineTaskListTests
    {
        string usersXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><users total_count=\"1\" offset=\"0\" limit=\"25\" type=\"array\"><user><id>1</id><login>lemon</login><firstname>Dmitry</firstname><lastname>Popov</lastname><mail>lemon@yandex.ru</mail><created_on>2013-06-13T21:30:03Z</created_on><last_login_on>2013-06-13T22:15:54Z</last_login_on></user></users>";
        string issuesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><issues total_count=\"1\" offset=\"0\" limit=\"25\" type=\"array\"><issue><id>1</id><project id=\"2\" name=\"Redmine API Library\"/><tracker id=\"2\" name=\"Feature\"/><status id=\"3\" name=\"Resolved\"/><priority id=\"2\" name=\"Normal\"/><author id=\"1\" name=\"Dmitry Popov\"/><assigned_to id=\"1\" name=\"Dmitry Popov\"/><subject>Parse Redmine API XML</subject><description>Users, projects and issues</description><start_date>2013-06-13</start_date><due_date>2013-06-14</due_date><done_ratio>100</done_ratio><estimated_hours>2</estimated_hours><created_on>2013-06-13T22:10:24Z</created_on><updated_on>2013-06-13T22:10:24Z</updated_on><closed_on>2013-06-14T00:15:03Z</closed_on></issue></issues>";
        
        string usersXmlCount3Offset0Limit1 = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><users total_count=\"3\" offset=\"0\" limit=\"1\" type=\"array\"><user><id>1</id><login>admin</login><firstname>Dmitry</firstname><lastname>Popov</lastname><mail>lemon@yandex.ru</mail><created_on>2013-06-13T21:30:03Z</created_on><last_login_on>2013-06-13T22:15:54Z</last_login_on></user></users>";
        string usersXmlCount3Offset1Limit1 = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><users total_count=\"3\" offset=\"1\" limit=\"1\" type=\"array\"><user><id>2</id><login>lemon</login><firstname>Dmitry</firstname><lastname>Popov</lastname><mail>lemon@yandex.ru</mail><created_on>2013-06-13T21:30:03Z</created_on><last_login_on>2013-06-13T22:15:54Z</last_login_on></user></users>";

        [SetUp]
        public void SetUp()
        {
            RedmineTaskList.ClearUserCache();
        }

        [Test]
        public void Get()
        {
            var request = MockRepository.GenerateMock<TestWebRequest>();
            var response = CreateValidResponseStub(usersXml, issuesXml);
            
            WebRequest.RegisterPrefix("test", TestWebRequest.GetCreator(request));
            
            request.Expect(x => x.Headers.Add("", "")).IgnoreArguments();
            request.Stub(x => x.GetResponse()).Return(response);
            
            var issues = RedmineTaskList.Get("lemon", "Pa$sw0rd", "test://redmine.org/");

            Assert.AreEqual(1, issues.Length);
            Assert.AreEqual("Parse Redmine API XML", issues[0].Subject);
        }

        [Test]
        public void Get_AssertUri()
        {
            var request = MockRepository.GenerateMock<TestWebRequest>();
            var response = CreateValidResponseStub(usersXml, issuesXml);

            WebRequest.RegisterPrefix("test", TestWebRequest.GetCreator(request));
            
            request.Expect(x => x.Create(new Uri("test://redmine.org/users.xml"))).Repeat.Once();
            request.Expect(x => x.Create(new Uri("test://redmine.org/issues.xml?assigned_to_id=1"))).Repeat.Once();
            request.Expect(x => x.Headers.Add("", "")).IgnoreArguments().Repeat.Twice();
            request.Expect(x => x.GetResponse()).Return(response).Repeat.Twice();
            
            RedmineTaskList.Get("lemon", "Pa$sw0rd", "test://redmine.org/");

            request.VerifyAllExpectations();
        }

        [Test]
        public void Get_AssertUserIdIsCached()
        {
            var request = MockRepository.GenerateMock<TestWebRequest>();
            var response = CreateValidResponseStub(usersXml, issuesXml, issuesXml);

            WebRequest.RegisterPrefix("test", TestWebRequest.GetCreator(request));
            
            request.Expect(x => x.Create(new Uri("test://redmine.org/users.xml"))).Repeat.Once();
            request.Expect(x => x.Create(new Uri("test://redmine.org/issues.xml?assigned_to_id=1"))).Repeat.Twice();
            request.Expect(x => x.Headers.Add("", "")).IgnoreArguments().Repeat.Times(3);
            request.Expect(x => x.GetResponse()).Return(response).Repeat.Times(3);
            
            RedmineTaskList.Get("lemon", "Pa$sw0rd", "test://redmine.org/");
            RedmineTaskList.Get("lemon", "Pa$sw0rd", "test://redmine.org/");

            request.VerifyAllExpectations();
        }

        [Test]
        public void Get_AssertUserIdIsCachedWithRespectToUrl()
        {
            var request = MockRepository.GenerateMock<TestWebRequest>();
            var response = CreateValidResponseStub(usersXml, issuesXml, usersXml, issuesXml);

            WebRequest.RegisterPrefix("test", TestWebRequest.GetCreator(request));
            
            request.Expect(x => x.Create(new Uri("test://redmine1.org/users.xml"))).Repeat.Once();
            request.Expect(x => x.Create(new Uri("test://redmine2.org/users.xml"))).Repeat.Once();
            request.Expect(x => x.Create(new Uri("test://redmine1.org/issues.xml?assigned_to_id=1"))).Repeat.Once();
            request.Expect(x => x.Create(new Uri("test://redmine2.org/issues.xml?assigned_to_id=1"))).Repeat.Once();
            request.Expect(x => x.Headers.Add("", "")).IgnoreArguments().Repeat.Times(4);
            request.Expect(x => x.GetResponse()).Return(response).Repeat.Times(4);
            
            RedmineTaskList.Get("lemon", "Pa$sw0rd", "test://redmine1.org/");
            RedmineTaskList.Get("lemon", "Pa$sw0rd", "test://redmine2.org/");

            request.VerifyAllExpectations();
        }

        [Test]
        public void Get_AssertUsersAreRequestedContinuouslyUntilLoginFound()
        {
            var request = MockRepository.GenerateMock<TestWebRequest>();
            var response = CreateValidResponseStub(usersXmlCount3Offset0Limit1, usersXmlCount3Offset1Limit1, issuesXml);

            WebRequest.RegisterPrefix("test", TestWebRequest.GetCreator(request));
            
            request.Expect(x => x.Create(new Uri("test://redmine.org/users.xml"))).Repeat.Once();
            request.Expect(x => x.Create(new Uri("test://redmine.org/users.xml?offset=1"))).Repeat.Once();
            request.Expect(x => x.Create(new Uri("test://redmine.org/issues.xml?assigned_to_id=2"))).Repeat.Once();
            request.Expect(x => x.Headers.Add("", "")).IgnoreArguments().Repeat.Times(3);
            request.Expect(x => x.GetResponse()).Return(response).Repeat.Times(3);
            
            RedmineTaskList.Get("lemon", "Pa$sw0rd", "test://redmine.org/");

            request.VerifyAllExpectations();
        }


        private static WebResponse CreateValidResponseStub(params string[] responseXml)
        {
            var response = MockRepository.GenerateStub<WebResponse>();
            var streams = new MemoryStream[responseXml.Length];
            
            for (int i = 0; i < streams.Length; i++)
			{
                streams[i] = new MemoryStream(Encoding.UTF8.GetBytes(responseXml[i]));
			}

            var streamIndex = 0;
            response.Stub(x => x.GetResponseStream()).Do((Func<Stream>)(() => {
                
                if (streamIndex >= streams.Length)
                {
                    throw new InvalidOperationException("Response is not stubbed");
                }

                return streams[streamIndex++];
            }));

            return response;
        }
    }
}
