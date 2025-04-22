using CrowdQR.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add API client configuration
builder.Services.AddHttpClient("CrowdQRApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5000");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<ApiService>(sp =>
{
    var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var client = clientFactory.CreateClient("CrowdQRApi");
    var logger = sp.GetRequiredService<ILogger<ApiService>>();
    var config = sp.GetRequiredService<IConfiguration>();
    return new ApiService(client, logger, config);
});
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<RequestService>();
builder.Services.AddScoped<VoteService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<DashboardService>();

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
builder.Services.AddScoped<SessionManager>();

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
