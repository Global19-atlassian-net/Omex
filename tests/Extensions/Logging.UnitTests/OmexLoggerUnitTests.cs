﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Microsoft.Omex.Extensions.Logging.Replayable;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Omex.Extensions.Logging.UnitTests
{
	[TestClass]
	public class OmexLoggerUnitTests
	{
		[TestMethod]
		public void MessagePropagatedToEventSource()
		{
			Mock<ILogsEventSource> eventSourceMock = CreateEventSourceMock(isEnabled: true);
			LogMessage(nameof(MessagePropagatedToEventSource), eventSourceMock);
			eventSourceMock.Verify(m_logExpression, Times.Once);
		}


		[TestMethod]
		public void IsEnabledUsed()
		{
			Mock<ILogsEventSource> eventSourceMock = CreateEventSourceMock(isEnabled: false);
			LogMessage(nameof(IsEnabledUsed), eventSourceMock);
			eventSourceMock.Verify(m_logExpression, Times.Never);
		}


		[TestMethod]
		public void IsReplayableMessageUsed()
		{
			string suffix = nameof(IsReplayableMessageUsed);
			Mock<ILogsEventSource> eventSourceMock = CreateEventSourceMock(isReplayable: false);

			ReplayableActivity activity = CreateActivity(suffix);
			activity.Start();
			LogMessage(nameof(IsReplayableMessageUsed), eventSourceMock);
			activity.Stop();

			eventSourceMock.Verify(m_logExpression, Times.Once);
			Assert.IsFalse(activity.GetLogEvents().Any(), "Log should not be stored for replay");
		}


		[TestMethod]
		public void ReplayedMessageSaved()
		{
			string suffix = nameof(ReplayedMessageSaved);
			Mock<ILogsEventSource> eventSourceMock = CreateEventSourceMock(isReplayable: true);

			EventId eventId = CreateEventId(7, suffix);
			ReplayableActivity activity = CreateActivity(suffix);
			activity.Start();
			LogMessage(nameof(ReplayedMessageSaved), eventSourceMock);
			activity.Stop();

			eventSourceMock.Verify(m_logExpression, Times.Once);
			LogMessageInformation info = activity.GetLogEvents().Single();

			Assert.AreEqual(GetLogCategory(suffix), info.Category);
			Assert.AreEqual(GetLogMessage(suffix), info.Message);
			Assert.AreEqual(eventId, info.EventId);
		}


		[TestMethod]
		public void ScopeProperlyCreated()
		{
			(ILogger logger, Mock<IExternalScopeProvider> scopeProvicedMock) = LogMessage(nameof(ReplayedMessageSaved), CreateEventSourceMock());

			object obj = new object();
			IDisposable resultMock = new Mock<IDisposable>().Object;
			scopeProvicedMock.Setup(p => p.Push(obj)).Returns(resultMock);

			IDisposable result = logger.BeginScope(obj);

			scopeProvicedMock.Verify(p => p.Push(obj), Times.Once);
			Assert.AreEqual(resultMock, result);
		}


		private Mock<ILogsEventSource> CreateEventSourceMock(bool isEnabled = true, bool isReplayable = true)
		{
			Mock<ILogsEventSource> eventSourceMock = new Mock<ILogsEventSource>();
			eventSourceMock.Setup(e => e.IsEnabled(It.IsAny<LogLevel>())).Returns(isEnabled);
			eventSourceMock.Setup(e => e.IsReplayableMessage(It.IsAny<LogLevel>())).Returns(isReplayable);
			return eventSourceMock;
		}


		private (ILogger, Mock<IExternalScopeProvider>) LogMessage(string suffix, Mock<ILogsEventSource> eventSourceMock)
		{
			Mock<IExternalScopeProvider> scopeProvicedMock = new Mock<IExternalScopeProvider>();
			ILogger logger = new OmexLogger(eventSourceMock.Object, scopeProvicedMock.Object, GetLogCategory(suffix));
			logger.LogError(GetLogMessage(suffix));
			return (logger, scopeProvicedMock);
		}


		private string GetLogMessage(string suffix) => $"Message-{suffix}";


		private string GetLogCategory(string suffix) => $"Category-{suffix}";


		private EventId CreateEventId(int id, string suffix) => new EventId(id, $"EventId-{suffix}");


		private ReplayableActivity CreateActivity(string suffix) => new ReplayableActivity($"Activity-{suffix}");


		private readonly Expression<Action<ILogsEventSource>> m_logExpression = e =>
			e.LogMessage(
				It.IsAny<string>(),
				It.IsAny<ActivityTraceId>(),
				It.IsAny<string>(),
				LogLevel.Error,
				It.IsAny<EventId>(),
				It.IsAny<int>(),
				It.IsAny<string>());
	}
}