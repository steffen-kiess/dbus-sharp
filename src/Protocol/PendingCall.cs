// Copyright 2007 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Threading;

namespace DBus.Protocol
{
	public class PendingCall : IAsyncResult
	{
		Connection conn;
		Message reply;
		ManualResetEvent waitHandle;
		bool completedSync;
		bool keepFDs;
		
		public event Action<Message> Completed;

		public PendingCall (Connection conn) : this (conn, false) {}
		public PendingCall (Connection conn, bool keepFDs)
		{
			this.conn = conn;
			this.keepFDs = keepFDs;
		}

		internal bool KeepFDs
		{
			get {
				return keepFDs;
			}
		}

		public Message Reply {
			get {
				if (reply != null)
					return reply;

				if (Thread.CurrentThread == conn.mainThread) {
					while (reply == null) {
						var msg = conn.ReadMessage ();
						if (msg == null)
							throw new Exception ("Connection closed while waiting for reply");
						conn.HandleMessage (msg);
					}

					completedSync = true;

					conn.DispatchSignals ();
				} else {
					if (waitHandle == null)
						Interlocked.CompareExchange (ref waitHandle, new ManualResetEvent (false), null);

					while (reply == null)
						waitHandle.WaitOne ();

					completedSync = false;
				}

				return reply;
			} 
			set {
				if (reply != null)
					throw new Exception ("Cannot handle reply more than once");

				reply = value;

				if (waitHandle != null)
					waitHandle.Set ();

				if (Completed != null)
					Completed (reply);
			}
		}

		public void Cancel ()
		{
			throw new NotImplementedException ();
		}

		#region IAsyncResult Members

		object IAsyncResult.AsyncState {
			get {
				return conn;
			}
		}

		WaitHandle IAsyncResult.AsyncWaitHandle {
			get {
				if (waitHandle == null)
					waitHandle = new ManualResetEvent (false);

				return waitHandle;
			}
		}

		bool IAsyncResult.CompletedSynchronously {
			get {
				return reply != null && completedSync;
			}
		}

		bool IAsyncResult.IsCompleted {
			get {
				return reply != null;
			}
		}

		#endregion
	}
}
