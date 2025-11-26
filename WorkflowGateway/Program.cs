using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 1. Ocelot Ayar Dosyasýný Tanýtýyoruz
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// 2. Ocelot Servisini Ekliyoruz
builder.Services.AddOcelot(builder.Configuration);

// 3. CORS (React'ýn Gateway'e eriþmesi için ÞART)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // React adresi
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// 4. CORS'u Aktif Et (Ocelot'tan önce!)
app.UseCors("AllowReactApp");

// 5. Ocelot Middleware'i Baþlat
await app.UseOcelot();

app.Run();
