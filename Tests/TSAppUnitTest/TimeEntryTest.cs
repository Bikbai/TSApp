using Microsoft.VisualStudio.TestTools.UnitTesting;
using TSApp.ViewModel;
using TSApp.StaticData;
using TSApp.Model;
using System;
using Newtonsoft.Json;
using TSApp.Behaviors;

namespace TSAppUnitTest
{
    [TestClass]
    public class TimeEntryTest
    {
        private static string CTE = "{" +
            "\r\n  \"id\": \"61afaee6be737841a55dc648\"," +
            "\r\n  \"workItemId\": 11716," +
            "\r\n  \"workTime\": \"02:00:00\"," +
            "\r\n  \"start\": \"2021-12-19T10:00:00\"," +
            "\r\n  \"end\": \"2021-12-19T12:00:00\"," +
            "\r\n  \"comment\": null" +
            "\r\n}";
        ClokifyEntry ce = JsonConvert.DeserializeObject<ClokifyEntry>(CTE);
        [TestMethod]
        public void TestStartTime()
        {        
            TimeEntry te = new TimeEntry((ClokifyEntry)ce);
            // с 10 до 12, 2 часа
            te.StartTime = "12:00";
            Assert.AreEqual("12:00", te.StartTime);
            Assert.AreEqual("14:00", te.EndTime);
            Assert.AreEqual("2:00", te.Work);
        }
        [TestMethod]
        public void TestEndTime()
        {
            TimeEntry te = new TimeEntry((ClokifyEntry)ce);
            // с 10 до 12, 2 часа
            te.EndTime = "17:00";
            Assert.AreEqual("10:00", te.StartTime, "StartTime");
            Assert.AreEqual("17:00", te.EndTime, "EndTime");
            Assert.AreEqual("7:00", te.Work, "Work");
        }
        [TestMethod]
        public void TestWorkTime()
        {
            TimeEntry te = new TimeEntry((ClokifyEntry)ce);
            // с 10 до 12, 2 часа
            te.Work = "5:00";
            Assert.AreEqual("10:00", te.StartTime);
            Assert.AreEqual("15:00", te.EndTime);
            Assert.AreEqual("5:00", te.Work);
        }
        [TestMethod]
        public void TestHighWorkTime()
        {
            TimeEntry te = new TimeEntry((ClokifyEntry)ce);
            // с 10 до 12, 2 часа
            te.Work = "15:00";
            Assert.AreEqual("10:00", te.StartTime);
            Assert.AreEqual("22:00", te.EndTime);
            Assert.AreEqual("12:00", te.Work);
        }
        [TestMethod]
        public void TestIntTime()
        {
            TimeEntry te = new TimeEntry((ClokifyEntry)ce);
            // с 10 до 12, 2 часа
            te.StartTime = "1.21";
            Assert.AreEqual("10:00", te.StartTime);
            Assert.AreEqual("12:00", te.EndTime);
            Assert.AreEqual("2:00", te.Work);
        }
        [TestMethod]
        public void FilterTest()
        {
            TimeEntry te = new TimeEntry((ClokifyEntry)ce);
            TimeEntryFilter f = new TimeEntryFilter();
            Assert.AreEqual(false, f.FilterRecords(te), "Пустой фильтр");
            f.Calday = new DateTime(2021, 12, 07);
            Assert.AreEqual(true, f.FilterRecords(te), "Указана дата.");
            f.WorkItemId = 11716;
            Assert.AreEqual(true, f.FilterRecords(te), "Указаны оба.");
            f.Calday = null;
            Assert.AreEqual(true, f.FilterRecords(te), "Указан Wi ID.");
        }
        [TestMethod]
        public void PublishTest()
        {
            ce.Description = "11716.Test record, to delete";
            DAL dal = new DAL();
            var a3 = dal.Init().Result;
            var a1 = dal.PerformClokiConnect().Result;
            var a2 = dal.UpdateClokiEntry(ce).Result;
        }

    }
}
