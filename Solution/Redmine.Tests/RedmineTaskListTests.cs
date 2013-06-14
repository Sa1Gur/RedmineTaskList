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
        [Test]
        public void Get()
        {
            var request = MockRepository.GenerateMock<TestWebRequest>();
            var response = CreateValidResponseStub();
            
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
            var response = CreateValidResponseStub();

            WebRequest.RegisterPrefix("test", TestWebRequest.GetCreator(request));
            
            request.Expect(x => x.Create(new Uri("test://redmine.org/users.xml"))).Repeat.Once();
            request.Expect(x => x.Create(new Uri("test://redmine.org/issues.xml?assigned_to_id=1"))).Repeat.Once();
            request.Expect(x => x.Headers.Add("", "")).IgnoreArguments().Repeat.Twice();
            request.Expect(x => x.GetResponse()).Return(response).Repeat.Twice();
            
            var issues = RedmineTaskList.Get("lemon", "Pa$sw0rd", "test://redmine.org/");

            request.VerifyAllExpectations();
        }

        private static WebResponse CreateValidResponseStub()
        {
            var response = MockRepository.GenerateStub<WebResponse>();
            var usersXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><users total_count=\"1\" offset=\"0\" limit=\"25\" type=\"array\"><user><id>1</id><login>lemon</login><firstname>Dmitry</firstname><lastname>Popov</lastname><mail>lemon@yandex.ru</mail><created_on>2013-06-13T21:30:03Z</created_on><last_login_on>2013-06-13T22:15:54Z</last_login_on></user></users>";
            var issuesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><issues total_count=\"1\" offset=\"0\" limit=\"25\" type=\"array\"><issue><id>1</id><project id=\"2\" name=\"Redmine API Library\"/><tracker id=\"2\" name=\"Feature\"/><status id=\"3\" name=\"Resolved\"/><priority id=\"2\" name=\"Normal\"/><author id=\"1\" name=\"Dmitry Popov\"/><assigned_to id=\"1\" name=\"Dmitry Popov\"/><subject>Parse Redmine API XML</subject><description>Users, projects and issues</description><start_date>2013-06-13</start_date><due_date>2013-06-14</due_date><done_ratio>100</done_ratio><estimated_hours>2</estimated_hours><created_on>2013-06-13T22:10:24Z</created_on><updated_on>2013-06-13T22:10:24Z</updated_on><closed_on>2013-06-14T00:15:03Z</closed_on></issue></issues>";
            var streams = new MemoryStream[] {
                new MemoryStream(Encoding.UTF8.GetBytes(usersXml)),
                new MemoryStream(Encoding.UTF8.GetBytes(issuesXml))
            };
            var i = 0;

            response.Stub(x => x.GetResponseStream()).Do((Func<Stream>)(() => streams[i++]));

            return response;
        }
    }
}
