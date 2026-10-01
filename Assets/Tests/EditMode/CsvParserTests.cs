using NUnit.Framework;
using WordRPG.Core;

namespace WordRPG.Tests
{
    public class CsvParserTests
    {
        [Test]
        public void SimpleRows()
        {
            var rows = CsvParser.Parse("id,english\na,apple\nb,banana");
            Assert.AreEqual(3, rows.Count);
            CollectionAssert.AreEqual(new[] { "b", "banana" }, rows[2]);
        }

        [Test]
        public void QuotedFieldKeepsComma()
        {
            var rows = CsvParser.Parse("abandon,\"버리다, 포기하다\",v");
            CollectionAssert.AreEqual(new[] { "abandon", "버리다, 포기하다", "v" }, rows[0]);
        }

        [Test]
        public void DoubledQuoteIsEscapedQuote()
        {
            var rows = CsvParser.Parse("\"He said \"\"hi\"\"\",x");
            Assert.AreEqual("He said \"hi\"", rows[0][0]);
        }

        [Test]
        public void HandlesBomCrLfBlankLinesAndEmptyFields()
        {
            var rows = CsvParser.Parse("﻿a,b,c\r\n\r\n1,,3\r\n,,\r\n");
            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("a", rows[0][0]);
            CollectionAssert.AreEqual(new[] { "1", "", "3" }, rows[1]);
        }
    }
}
