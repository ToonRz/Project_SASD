using Microsoft.EntityFrameworkCore;

namespace Webboard.DataAccess;

public class WebboardDbContext(DbContextOptions<WebboardDbContext> options) : DbContext(options);
