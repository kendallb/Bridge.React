﻿using System;

namespace Bridge.React
{
	public sealed class AppDispatcher : IDispatcher
	{
		// TODO: Change this to an Action<IDispatcherAction> event handler later on when removing the DispatcherMessage support altogether.
		// It can't be changed now because consumers may be relying on the extra information given in the DispatcherMessage class.
		private event Action<DispatcherMessage> _dispatcher;

		private bool _currentDispatching;
		public AppDispatcher()
		{
			_currentDispatching = false;
		}

		/// <summary>
		/// Dispatches an action that will be sent to all callbacks registered with this dispatcher.
		/// </summary>
		/// <param name="action">The action to dispatch; may not be null.</param>
		public void Dispatch(IDispatcherAction action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			// The obsolete MessageSourceOptions handling needs to stay in the meantime, so just dispatch this as any arbitrary source.
			// Eventually this method should absorb the Dispatch(DispatcherMessage message) method's behaviour but dispatch the action
			// directly down to the event handler without wrapping it in a DispatcherMessage.
			Dispatch(new DispatcherMessage(MessageSourceOptions.View, action));
		}

		/// <summary>
		/// Registers a callback to receive actions dispatched through this dispatcher.
		/// </summary>
		/// <param name="callback">The callback; may not be null.</param>
		/// <remarks>
		/// Actions will be sent to each receiver in the same order as which the receivers called Register.
		/// </remarks>
		public void Register(Action<IDispatcherAction> callback)
		{
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));

			// We have to keep support for the DispatcherMessage class for now (until a breaking release is made), so the _dispatcher
			// event handler has to take DispatcherMessages for now, and we have to wrap the callback given here.
			_dispatcher += obsoleteDispatcherMessage => callback(obsoleteDispatcherMessage.Action);
		}

		/// <summary>
		/// Actions will sent to each receiver in the same order as which the receivers called Register.
		/// </summary>
		[Obsolete("Support for Actions attributed to different sources (i.e. View vs. Server actions) will be removed from the IDispatcher interface. Use the Register(Action<IDispatcherAction>) method instead of Register(Action<DispatcherMessage>).")]
		public void Register(Action<DispatcherMessage> callback)
		{
			_dispatcher += callback;
		}

		[Obsolete("Support for Actions attributed to different sources (i.e. View vs. Server actions) will be removed from the IDispatcher interface. Use the Dispatch method instead of HandleViewAction or HandleServerAction.")]
		public void HandleViewAction(IDispatcherAction action)
		{
			if (action == null)
				throw new ArgumentNullException("action");

			Dispatch(new DispatcherMessage(MessageSourceOptions.View, action));
		}

		[Obsolete("Support for Actions attributed to different sources (i.e. View vs. Server actions) will be removed from the IDispatcher interface. Use the Dispatch method instead of HandleViewAction or HandleServerAction.")]
		public void HandleServerAction(IDispatcherAction action)
		{
			if (action == null)
				throw new ArgumentNullException("action");

			Dispatch(new DispatcherMessage(MessageSourceOptions.Server, action));
		}

		private void Dispatch(DispatcherMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			// Dispatching a message during the handling of another is not allowed, in order to be consistent with the Facebook Dispatcher
			// (see https://github.com/facebook/flux/blob/master/src/Dispatcher.js#L183)
			if (_dispatcher != null)
			{
				if (_currentDispatching)
					throw new Exception("Cannot dispatch in the middle of a dispatch.");
				_currentDispatching = true;
				try
				{
					_dispatcher(message);
				}
				finally
				{
					_currentDispatching = false;
				}
			}
		}
	}
}