﻿using System;
using CarrierPidgin.Lib;
using CarrierPidgin.ServiceA;
using CarrierPidgin.ServiceA.Statistics;
using FluentAssertions;
using NUnit.Framework;
using Optional;

namespace tests
{
    public class DeDupHandlerTests
    {
        public static string Domain = "statistics";
        public static string ConnectionString;

         static DeDupHandlerTests()
         {
             ConnectionString = TestConfig.GetConnectionString(Domain);
         }

        [Test]
        public void Handle_Creates_Entry_Where_None_Exists_For_A_Queue()
        {
            MessageStreamName queueName = new MessageStreamName(Guid.NewGuid().ToString());
            long messageNumber = 1;

            using (var uow = new UnitOfWork(ConnectionString))
            {
                var lastProcessedEntry = MessageNumberRepository.GetLastProcessedMessageNumber(
                    uow, 
                    queueName);
                Assert.IsTrue(!lastProcessedEntry.HasValue);
            }

            using (var uow = new UnitOfWork(ConnectionString))
            {
                var handler = new DeDupHandler<object>(uow);
                handler._next = _ => { };
                handler.Handle(new{}, queueName, messageNumber);
                uow.Commit();
            }

            using (var uow = new UnitOfWork(ConnectionString))
            {
                Option<Tuple<long, MessageQueueProcessingDetailsRow>> lastProcessedEntry = MessageNumberRepository.GetLastProcessedMessageNumber(uow, queueName);
                Assert.IsTrue(lastProcessedEntry.HasValue);
                var lastMessageNumber = lastProcessedEntry.GetValue().Item1;
                lastMessageNumber.Should().Be(1);
            }
        }
    }

    public static class OptionalExtensions
    {
        public static T GetValue<T>(this Option<T> o)
        {
            return o.Match(
                x => x, () => throw new Exception("Option type should be 'Some' but is 'None'"));
        }
        
    }
}