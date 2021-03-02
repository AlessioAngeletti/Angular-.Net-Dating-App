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
            return await _context.Users.Include (p => p.Photos).FirstOrDefaultAsync (u => u.Id == id);
        }

        public async Task<PagedList<User>> GetUsers (UserParams userParams) {

            var query = _context.Users.Include (p => p.Photos).OrderByDescending (u => u.LastActive).AsQueryable ();

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

            var user = await _context.Users.Include (x => x.Likers).Include (x => x.Likees).SingleOrDefaultAsync (u => u.Id == id);

            if (likers)
                return user.Likers.Where (u => u.LikeeId == id).Select (i => i.LikerId);
            else
                return user.Likees.Where (u => u.LikerId == id).Select (i => i.LikeeId);
        }

        public async Task<bool> SaveAll () {
            return await _context.SaveChangesAsync () > 0;
        }
    }
}