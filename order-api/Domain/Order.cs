﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CarrierPidgin.OrderService.Messages;

namespace CarrierPidgin.OrderService.Domain
{

    public class Order
    {
        public enum State
        {
            Cancelled = 1,
            Active = 2
        }
        // Public properties
        public Guid OrderNumber { get; private set; }
        public string Description { get; private  set; }
        public State Status { get; private set; }

        // Public operations
        public Order(Guid orderNumber, string description)
        {
            var e = new OrderPlacedEvent()
            {
                OrderNumber = orderNumber,
                Description = description,
                Version = 0
            };

            this.Apply(e, true);
        }

        public void Cancel()
        {
            var e = new OrderCancelledEvent()
            {
                OrderNumber = this.OrderNumber,
                Version = LastVersionNumber() + 1
            };
            Apply(e, true);
        }

        public void Activate()
        {
            var e = new OrderActivatedEvent()
            {
                OrderNumber = this.OrderNumber,
                Version = LastVersionNumber() + 1
            };
            Apply(e, true);
        }

        // Reconstitute object from events
        public static Order Build(List<IOrderEvent> evts)
        {
            return evts.Count > 0
                ? new Order(evts)
                : null;
        }

        private Order(List<IOrderEvent> evts)
        {
            evts.ForEach(e => EventRouter[e.GetType()](this, e));
        }

        // Object state management
        private void AddEvent(IOrderEvent e, bool isNew)
        {
            _orderEvents.Add(e);
            if (isNew)
                _newOrderEvents.Add(e);
        }

        private void Apply(OrderCancelledEvent e, bool isNew)
        {
            if (Status == State.Cancelled)
                return;
            AddEvent(e, isNew);
            Status = State.Cancelled;
        }

        private void Apply(OrderActivatedEvent e, bool isNew)
        {
            if (Status == State.Active)
                return;
            AddEvent(e, isNew);
            Status = State.Active;
        }

        private void Apply(OrderPlacedEvent e, bool isNew)
        {
            AddEvent(e, isNew);
            this.Description = e.Description;
            this.OrderNumber = e.OrderNumber;
            this.Status = State.Active;
        }

        private static readonly int NoEventsEventNumber = -1;
        private int LastVersionNumber()
        {
            return _orderEvents
                .Select(x => x.Version)
                .Concat(new[] {NoEventsEventNumber})
                .Max();
        }

        public static IDictionary<Type, Action<Order, IOrderEvent>> EventRouter = new Dictionary<Type, Action<Order, IOrderEvent>>()
        {
                {typeof(OrderPlacedEvent), (o, e) => o.Apply((OrderPlacedEvent) e, false)},
                {typeof(OrderCancelledEvent), (o, e) => o.Apply((OrderCancelledEvent) e, false)},
                {typeof(OrderActivatedEvent), (o, e) => o.Apply((OrderActivatedEvent) e, false)}
        };

        private readonly List<IOrderEvent> _orderEvents = new List<IOrderEvent>();
        private readonly List<IOrderEvent> _newOrderEvents = new List<IOrderEvent>();

        // For persistence mechanism to get newly added events.
        public List<IOrderEvent> NewEvents()
        {
            return _newOrderEvents.ToList();
        }
    }
}