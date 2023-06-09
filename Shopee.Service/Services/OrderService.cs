using Shopee.Data.IRepositories;
using Shopee.Data.Repositories;
using Shopee.Domain.Entities;
using Shopee.Domain.Enums;
using Shopee.Service.DTOs.OrderItems;
using Shopee.Service.DTOs.Orders;
using Shopee.Service.DTOs.Products;
using Shopee.Service.Interfaces;
using System.Linq.Expressions;

namespace Shopee.Service.Services;

public class OrderService : IOrderService
{
    private IOrderRepository orderRepo = new OrderRepository();
    private IOrderItemService orderItemService = new OrderItemService();
    private IOrderItemRepository orderItemRepo = new OrderItemRepository();
    private IPaymentService paymentService = new PaymentService();
    private IUserRepository userRepo = new UserRepository();
    private ICartService cartService = new CartService();
    private ICartRepository cartRepo = new CartRepository();
    private IProductRepository productRepo = new ProductRepository();
    public async Task<OrderViewDto> ChangeOrderStatusAsync(long orderId, OrderStatus newStatus)
    {
        var entity = await this.orderRepo.GetAsync(u => u.Id == orderId);
        if (entity is null) return null;

        entity.Status = newStatus;
        entity.UpdatedAt = DateTime.UtcNow;

        await this.orderRepo.UpdateAsync(entity);

        await this.orderRepo.SaveChangesAsync();

        var orderItemDtos = await orderItemService.GetAllAsync(o => o.OrderId == orderId);
        var totalPrice = 0m;
        foreach (var item in orderItemDtos)
        {
            totalPrice += item.TotalPrice;
        }

        return new OrderViewDto
        {
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Id = entity.Id,
            Payment = await paymentService.GetByIdAsync(entity.PaymentId),
            TotalPrice = totalPrice,
            UserId = entity.UserId
        };
    }

    public async Task<OrderViewDto> CreateAsync(OrderCreationDto dto)
    {
        var user = await userRepo.GetAsync(u => u.Id == dto.UserId);
        if (user is null)
            return null;

        var cart = await cartService.GetByUserIdAsync(user.Id);
        foreach (var item in cart.Items)
            if (item.Count > item.Product.Count)
                return null;

        var payment = await paymentService.CreateAsync(dto.Payment);
        if (payment is null)
            return null;

        var createdOrder = await orderRepo.CreateAsync(new Order
        {
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id,
            PaymentId = payment.Id
        });

        var orderId = (long)(await orderRepo.GetAllASync()).Count + 1;
        foreach (var item in cart.Items)
        {
            var orderItem = await orderItemRepo.GetAsync(o => o.Id == item.Id);
            orderItem.OrderId = orderId;
            await orderItemRepo.UpdateAsync(orderItem);

            var product = await productRepo.GetAsync(p => p.Id == item.Product.Id);
            product.Count -= item.Count;
            await productRepo.UpdateAsync(product);
        }

        var cartEntity = await cartRepo.GetAsync(c => c.Id == user.CartId);
        cartEntity.OrderItemIds = new List<long>();
        await cartRepo.UpdateAsync(cartEntity);
        

        var mappedDto = new OrderViewDto
        {
            Status = createdOrder.Status,
            CreatedAt = createdOrder.CreatedAt,
            UserId = createdOrder.UserId,
            Payment = payment,
            TotalPrice = cart.TotalPrice,
            Items = cart.Items
        };

        await orderItemRepo.SaveChangesAsync();
        await orderRepo.SaveChangesAsync();
        await cartRepo.SaveChangesAsync();
        await userRepo.SaveChangesAsync();
        await productRepo.SaveChangesAsync();
        return mappedDto;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var entity = await orderRepo.GetAsync(c => c.Id == id);

        if (entity is null)
            return false;

        await orderRepo.DeleteAsync(o => o.Id == id);
        await this.orderRepo.SaveChangesAsync();
        return true;
    }

    public async Task<List<OrderViewDto>> GetAllAsync(Expression<Func<Order, bool>> expression)
    {
        var mappedDtos = new List<OrderViewDto>();

        foreach (var item in await orderRepo.GetAllASync(expression))
        {
            var orderItems = await orderItemService.GetAllAsync(o => o.OrderId == item.Id);
            var totalPrice = 0m;
            foreach (var i in orderItems)
                totalPrice += i.TotalPrice;

            mappedDtos.Add(new OrderViewDto
            {
                Status = item.Status,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                Id = item.Id,
                Items = orderItems,
                Payment = await paymentService.GetByIdAsync(item.PaymentId),
                UserId = item.UserId,
                TotalPrice = totalPrice
            });
        }

        return mappedDtos;
    }

    public async Task<OrderViewDto> GetByIdAsync(long id)
    {
        var entity = await orderRepo.GetAsync(o => o.Id == id);
        if (entity is null) return null;

        var orderItems = await orderItemService.GetAllAsync(o => o.OrderId == entity.Id);
        var totalPrice = 0m;
        foreach (var i in orderItems)
            totalPrice += i.TotalPrice;

        return new OrderViewDto
        {
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Id = entity.Id,
            Items = orderItems,
            Payment = await paymentService.GetByIdAsync(entity.PaymentId),
            UserId = entity.UserId,
            TotalPrice = totalPrice
        };
    }
}
