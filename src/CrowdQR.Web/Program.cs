var builder = WebApplication.CreateBuilder(args);

// Add API client configuration
builder.Services.AddHttpClient();
builder.Services.AddScoped<CrowdQR.Web.Services.ApiService>();
builder.Services.AddScoped<CrowdQR.Web.Services.EventService>();
builder.Services.AddScoped<CrowdQR.Web.Services.RequestService>();
builder.Services.AddScoped<CrowdQR.Web.Services.VoteService>();
builder.Services.AddScoped<CrowdQR.Web.Services.UserService>();
builder.Services.AddScoped<CrowdQR.Web.Services.SessionService>();
builder.Services.AddScoped<CrowdQR.Web.Services.DashboardService>();

// Add session state for user identification
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add session manager
builder.Services.AddScoped<CrowdQR.Web.Services.SessionManager>();

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
