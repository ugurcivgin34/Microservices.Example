﻿using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Order.API.Models;
using Order.API.Models.Enums;
using Order.API.ViewModels;
using Shared.Events;

namespace Order.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly OrderAPIDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;
        public OrdersController(OrderAPIDbContext context, IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(CreateOrderVM createOrder)
        {
           Models.Entities.Order order = new ()
           {
              OrderId = Guid.NewGuid(),
              BuyerId = createOrder.BuyerId,
              CreatedDate = DateTime.Now,
              OrderStatus = OrderStatus.Suspend,
           };

            order.OrderItems = createOrder.OrderItems.Select(x => new Models.Entities.OrderItem
            {
                Count = x.Count,
                Price = x.Price,
                ProductId = x.ProductId
            }).ToList();

            order.TotalPrice = order.OrderItems.Sum(x => x.Price * x.Count);

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            OrderCreatedEvent orderCreatedEvent = new()
            {
                BuyerId = order.BuyerId,
                OrderId = order.OrderId,
                OrderItems = order.OrderItems.Select(oi => new Shared.Messages.OrderItemMessage
                {
                    Count = oi.Count,
                    ProductId = oi.ProductId,
                }).ToList()
            };

            await _publishEndpoint.Publish(orderCreatedEvent);

            return Ok();
        }
    }
}
