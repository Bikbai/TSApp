using Microsoft.VisualStudio.TestTools.UnitTesting;
using TSApp.StaticData;
using System;

namespace TSAppUnitTest
{
    [TestClass]
    public class SettingsTest
    {
        bool MustInitPassed = false;

        [TestMethod]
        public void TestFirstRun()
        {
            Settings.value.MustInit += Value_MustInit;
            Settings.EraseFile();
            Settings.Load();
            Assert.AreEqual(true, MustInitPassed, "Первичная инициализация");
            Settings.Save();
            MustInitPassed = false;
            Settings.Load();
            Assert.AreEqual(false, MustInitPassed, "Загрузка из файла");
        }
        [TestMethod]
        public void TestLoad()
        {
            MustInitPassed = false;
            Settings.Load();
            Assert.AreEqual(false, MustInitPassed, "Загрузка из файла");
        }


        private void Value_MustInit(string msg)
        {
            MustInitPassed = true;
        }
    }
}
