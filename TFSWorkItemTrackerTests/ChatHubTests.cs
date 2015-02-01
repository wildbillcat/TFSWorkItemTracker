using System;
using TFSWorkItemTracker.Hubs;
using NUnit.Framework;

namespace TFSWorkItemTrackerTests
{
    [TestFixture()]
    public class ChatHubTests
    {
        [SetUp()]
        public void Init()
        {

        }

        [TearDown()]
        public void Clean()
        {

        }

        [Test]
        public void Test1()
        {
            //Arrange
            string query = "Select [ID], [Team Project], [State], [Changed Date], [Title] From WorkItems Where [State] = 'New'";
            //Act
            //query = query.Substring()
            //Assert
        }

        //[Test]
        public void Test2()
        {
            //Arrange

            //Act

            //Assert
        }
    }
}

