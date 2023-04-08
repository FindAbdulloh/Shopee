﻿using Microsoft.EntityFrameworkCore;
using Shopee.Data.DbContexts;
using Shopee.Data.IRepositories;
using Shopee.Domain.Entities;
using System.Linq.Expressions;

namespace Shopee.Data.Repositories;
public class MessageRepository : IMessageRepository
{
    private ShopeDbContext context = new ShopeDbContext();
    public async Task<Message> CreateAsync(Message message)
    {
        var userForInsert = await this.context.Messages.AddAsync(message);
        return userForInsert.Entity;
    }

    public async Task<bool> DeleteAsync(Expression<Func<Message, bool>> expression)
    {
        var MessageForDelete = await this.context.Messages.FirstOrDefaultAsync(expression);

        this.context.Messages.Remove(MessageForDelete);
        return true;
    }

    public async Task<List<Message>> GetAllASync(Expression<Func<Message, bool>> expression = null)
        => await this.context.Messages.ToListAsync();

    public async Task<Message> GetAsync(Expression<Func<Message, bool>> expression)
    => await this.context.Messages.FirstOrDefaultAsync(expression);

    public async Task<Message> UpdateAsync(Message message)
    {
        var messageForUpdate = await this.context.Messages.FirstOrDefaultAsync(u => u.Id == message.Id);

        messageForUpdate.Text = message.Text;
        messageForUpdate.UpdatedAt = DateTime.UtcNow;

        return messageForUpdate;
    }

    public async Task<bool> SaveChangesAsync()
            => 0 < (await context.SaveChangesAsync());

}