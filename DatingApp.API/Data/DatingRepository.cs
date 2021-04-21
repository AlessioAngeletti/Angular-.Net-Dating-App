using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data {
    public class DatingRepository : IDatingRepository {
        private readonly DataContext _context;

        public DatingRepository (DataContext context) {
            _context = context;
        }

        public void Add<T> (T entity) where T : class {
            _context.Add (entity);
        }

        public void Delete<T> (T entity) where T : class {
            _context.Remove (entity);
        }

        public async Task<Like> GetLike (int userId, int recipientId) {
            return await _context.Likes.SingleOrDefaultAsync (u => u.LikerId == userId && u.LikeeId == recipientId);
        }

        public async Task<Photo> GetMainPhotoForUser (int userId) {
            return await _context.Photos.Where (u => u.UserId == userId).FirstOrDefaultAsync (p => p.IsMain);
        }

        public async Task<Photo> GetPhoto (int id) {
            return await _context.Photos.FirstOrDefaultAsync (p => p.Id == id);
        }

        public async Task<User> GetUser (int id) {
            return await _context.Users.FirstOrDefaultAsync (u => u.Id == id);
        }

        public async Task<PagedList<User>> GetUsers (UserParams userParams) {

            var query = _context.Users.OrderByDescending (u => u.LastActive).AsQueryable ();

            query = query.Where (u => u.Id != userParams.UserId);
            query = query.Where (u => u.Gender == userParams.Gender);

            if (userParams.Likers) {
                var userLikers = await GetUserLikes (userParams.UserId, userParams.Likers);
                query = query.Where (u => userLikers.Contains (u.Id));
            }

            if (userParams.Likees) {
                var userLikees = await GetUserLikes (userParams.UserId, userParams.Likers);
                query = query.Where (u => userLikees.Contains (u.Id));
            }

            if (userParams.MinAge != 18 || userParams.MaxAge != 99) {
                var minDate = DateTime.Today.AddYears (-userParams.MaxAge - 1);
                var maxDate = DateTime.Today.AddYears (-userParams.MinAge);

                query = query.Where (u => u.DateOfBirth >= minDate && u.DateOfBirth <= maxDate);
            }

            if (!string.IsNullOrEmpty (userParams.OrderBy)) {
                switch (userParams.OrderBy) {
                    case "created":
                        query = query.OrderByDescending (u => u.Created);
                        break;
                }
            }

            return await PagedList<User>.CreateAsync (query, userParams.PageNumber, userParams.PageSize);
        }

        private async Task<IEnumerable<int>> GetUserLikes (int id, bool likers) {

            var user = await _context.Users.SingleOrDefaultAsync (u => u.Id == id);

            if (likers)
                return user.Likers.Where (u => u.LikeeId == id).Select (i => i.LikerId);
            else
                return user.Likees.Where (u => u.LikerId == id).Select (i => i.LikeeId);
        }

        public async Task<bool> SaveAll () {
            return await _context.SaveChangesAsync () > 0;
        }

        public async Task<Message> GetMessage (int id) {
            return await _context.Messages.FirstOrDefaultAsync (m => m.Id == id);
        }

        public async Task<PagedList<Message>> GetMessagesForUser (MessageParams messageParams) {

            var messages = _context.Messages.AsQueryable ();

            switch (messageParams.MessageContainer) {
                case "Inbox":
                    messages = messages.Where (u => u.RecipientId == messageParams.UserId && u.RecipientDeleted == false);
                    break;

                case "Outbox":
                    messages = messages.Where (u => u.SenderId == messageParams.UserId && u.SenderDeleted == false);
                    break;

                default:
                    messages = messages.Where (u => u.RecipientId == messageParams.UserId && u.RecipientDeleted == false && u.IsRead == false);
                    break;
            }

            messages = messages.OrderByDescending (d => d.MessageSent);

            return await PagedList<Message>.CreateAsync (messages, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<Message>> GetMessageThread (int userId, int recipientId) {

            var messages = await _context.Messages
                .Where (m => m.RecipientId == userId && m.RecipientDeleted == false && m.SenderId == recipientId ||
                    m.RecipientId == recipientId && m.SenderId == userId && m.SenderDeleted == false)
                .OrderByDescending (m => m.MessageSent)
                .ToListAsync ();

            return messages;
        }
    }
}