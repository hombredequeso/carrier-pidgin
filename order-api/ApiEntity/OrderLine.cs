﻿using System;

namespace Hdq.OrderApi.ApiEntity
{
    public class OrderLine
    {
        public Guid ItemId { get; set; }
        public int Quantity { get; set; }
        public decimal PricePerItem { get; set; }
    }
}