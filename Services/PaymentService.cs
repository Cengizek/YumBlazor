﻿using Microsoft.AspNetCore.Components;
using Stripe;
using Stripe.Checkout;
using YumBlazor.Data;
using YumBlazor.Repository.IRepository;
using YumBlazor.Utility;

namespace YumBlazor.Services
{
    public class PaymentService
    {
        private readonly NavigationManager _navigationManager;
        private readonly IOrderRepository _orderRepository;

        public PaymentService(NavigationManager navigationManager, IOrderRepository orderRepository)
        {
            _navigationManager = navigationManager;
            _orderRepository = orderRepository;
        }

        public Session CreateCheckoutSession(OrderHeader orderHeader)
        {
            var lineItems = orderHeader.OrderDetails
                .Select(order => new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmountDecimal = (decimal)order.Price * 100, //20.15_>2015
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = order.ProductName
                        }
                    },
                    Quantity = order.Count
                }).ToList();

            var options = new Stripe.Checkout.SessionCreateOptions
            {
                SuccessUrl = $"{_navigationManager.BaseUri}order/confirmation/{{CHECKOUT_SESSION_ID}}",
                CancelUrl =$"{_navigationManager.BaseUri}cart",
                LineItems = lineItems,
                Mode = "payment",
            };

            var service = new Stripe.Checkout.SessionService();
            Session session = service.Create(options);

            return session;
        }

        public async Task<OrderHeader> CheckPaymentStatusandUpdateOrder(string sessionId)
        {
            OrderHeader orderHeader = await _orderRepository.GetOrderBySessionIdAsync(sessionId);
            var service = new SessionService();
            var session =service.Get(sessionId);//1
            if(session.PaymentStatus.ToLower() == "paid")
            {
                await _orderRepository.UpdateStatusAsync(orderHeader.Id,SD.StatusApproved,session.PaymentIntentId);
            }
        
            return orderHeader;
        }

    }
}
