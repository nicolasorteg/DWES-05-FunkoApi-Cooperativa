using FluentAssertions;
using FunkoApi.Dto.Funko;
using FunkoApi.Mapper;
using FunkoApi.Models;
using MongoDB.Bson;

namespace FunkoApi.Tests.Mappers;

[TestFixture]
[TestOf(typeof(FunkoMapper))]
public class FunkoMapperTest {

    [TestFixture]
    public class CasosCorrectos {

        private Funko _funko = null!;
        private Categoria _categoria = null!;
        private Proveedor _proveedor = null!;

        [SetUp]
        public void SetUp() {

            _categoria = new Categoria {
                Id = "dQw4w9WgXcQ",
                Nombre = "Marvel"
            };

            _proveedor = new Proveedor {
                Id = new ObjectId("507f1f77bcf86cd799439011"),
                Nombre = "Funko Inc",
                Email = "contacto@funko.com",
                Telefono = "600123456"
            };

            _funko = new Funko {
                Id = 1,
                Nombre = "Iron Man",
                Precio = 19.99m,
                Stock = 10,
                Imagen = "/images/iron-man.png",
                CategoriaId = _categoria.Id,
                Categoria = _categoria,
                ProveedorId = _proveedor.Id.ToString(),
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc)
            };
        }

        // ---------- ToResponseDto ----------

        [Test]
        public void ToResponseDto_DeberiaMapearCamposYAnidarCategoriaYProveedor() {

            // act
            var res = _funko.ToResponseDto(_proveedor);

            // assert
            res.Id.Should().Be(1);
            res.Nombre.Should().Be("Iron Man");
            res.Precio.Should().Be(19.99m);
            res.Stock.Should().Be(10);
            res.Imagen.Should().Be("/images/iron-man.png");
            res.CreatedAt.Should().Be(_funko.CreatedAt);
            res.UpdatedAt.Should().Be(_funko.UpdatedAt);

            res.ResponseCategoria.Id.Should().Be("dQw4w9WgXcQ");
            res.ResponseCategoria.Nombre.Should().Be("Marvel");

            res.ResponseProveedor.Id.Should().Be("507f1f77bcf86cd799439011");
            res.ResponseProveedor.Nombre.Should().Be("Funko Inc");
            res.ResponseProveedor.Email.Should().Be("contacto@funko.com");
            res.ResponseProveedor.Telefono.Should().Be("600123456");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ToResponseDto_SinImagen_DeberiaUsarImagenPorDefecto(string? imagen) {

            // arrange
            _funko.Imagen = imagen;

            // act
            var res = _funko.ToResponseDto(_proveedor);

            // assert
            res.Imagen.Should().Be("/images/default-funko.png");
        }

        [Test]
        public void ToResponseDto_SinUpdatedAt_DeberiaDevolverUpdatedAtNull() {

            // arrange
            _funko.UpdatedAt = null;

            // act
            var res = _funko.ToResponseDto(_proveedor);

            // assert
            res.UpdatedAt.Should().BeNull();
        }

        // ---------- ToEntity ----------

        [Test]
        public void ToEntityFromCreateDto_DeberiaMapearCamposYAsignarCreatedAt() {

            // arrange
            var dto = new CreateFunkoDto(
                "Spider-Man", 24.5m, 5, "dQw4w9WgXcQ", "507f1f77bcf86cd799439011", "/images/spider.png");

            // act
            var res = dto.ToEntity();

            // assert
            res.Id.Should().Be(0);
            res.Nombre.Should().Be("Spider-Man");
            res.Precio.Should().Be(24.5m);
            res.Stock.Should().Be(5);
            res.CategoriaId.Should().Be("dQw4w9WgXcQ");
            res.ProveedorId.Should().Be("507f1f77bcf86cd799439011");
            res.Imagen.Should().Be("/images/spider.png");
            res.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            res.UpdatedAt.Should().BeNull();
            res.IsDeleted.Should().BeFalse();
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ToEntityFromCreateDto_ConImagenVacia_DeberiaGuardarImagenNull(string? imagen) {

            // arrange
            var dto = new CreateFunkoDto(
                "Spider-Man", 24.5m, 5, "dQw4w9WgXcQ", "507f1f77bcf86cd799439011", imagen);

            // act
            var res = dto.ToEntity();

            // assert
            res.Imagen.Should().BeNull();
        }

        // ---------- UpdateEntity (PUT completo) ----------

        [Test]
        public void UpdateEntity_DeberiaReemplazarTodosLosCamposYActualizarUpdatedAt() {

            // arrange
            var dto = new UpdateFunkoDto(
                "Thor", 29.99m, 3, "nuevaCategoria", "nuevoProveedor", "/images/thor.png");

            // act
            dto.UpdateEntity(_funko);

            // assert
            _funko.Nombre.Should().Be("Thor");
            _funko.Precio.Should().Be(29.99m);
            _funko.Stock.Should().Be(3);
            _funko.CategoriaId.Should().Be("nuevaCategoria");
            _funko.ProveedorId.Should().Be("nuevoProveedor");
            _funko.Imagen.Should().Be("/images/thor.png");
            _funko.UpdatedAt.Should().NotBeNull();
            _funko.UpdatedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Test]
        public void UpdateEntity_ConStockCero_DeberiaActualizarStockACero() {

            // arrange
            var dto = new UpdateFunkoDto("Thor", 29.99m, 0, "x", "y");

            // act
            dto.UpdateEntity(_funko);

            // assert
            _funko.Stock.Should().Be(0);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void UpdateEntity_ConImagenVacia_DeberiaGuardarImagenNull(string? imagen) {

            // arrange
            var dto = new UpdateFunkoDto("Thor", 29.99m, 3, "x", "y", imagen);

            // act
            dto.UpdateEntity(_funko);

            // assert (PUT: la imagen anterior se reemplaza, no se conserva)
            _funko.Imagen.Should().BeNull();
        }

        [Test]
        public void UpdateEntity_NoDeberiaModificarIdCreatedAtNiBorrado() {

            // arrange
            var dto = new UpdateFunkoDto("Thor", 29.99m, 3, "x", "y", "/images/thor.png");
            var createdAt = _funko.CreatedAt;

            // act
            dto.UpdateEntity(_funko);

            // assert
            _funko.Id.Should().Be(1);
            _funko.CreatedAt.Should().Be(createdAt);
            _funko.IsDeleted.Should().BeFalse();
            _funko.DeletedAt.Should().BeNull();
        }
    }

    [TestFixture]
    public class CasosIncorrectos {

        private Funko _funko = null!;
        private Proveedor _proveedor = null!;

        [SetUp]
        public void SetUp() {

            _proveedor = new Proveedor {
                Id = ObjectId.GenerateNewId(),
                Nombre = "Funko Inc",
                Email = "contacto@funko.com"
            };

            _funko = new Funko {
                Id = 1,
                Nombre = "Iron Man",
                Precio = 19.99m,
                Stock = 10,
                Categoria = new Categoria { Id = "dQw4w9WgXcQ", Nombre = "Marvel" }
            };
        }

        [Test]
        public void ToResponseDto_CuandoCategoriaEsNull_DeberiaLanzarNullReferenceException() {

            // arrange
            _funko.Categoria = null!;

            // act
            Action act = () => _funko.ToResponseDto(_proveedor);

            // assert
            act.Should().Throw<NullReferenceException>();
        }

        [Test]
        public void ToResponseDto_CuandoProveedorEsNull_DeberiaLanzarNullReferenceException() {

            // act
            Action act = () => _funko.ToResponseDto(null!);

            // assert
            act.Should().Throw<NullReferenceException>();
        }

        [Test]
        public void ToEntityFromCreateDto_CuandoDtoEsNull_DeberiaLanzarNullReferenceException() {

            // arrange
            CreateFunkoDto dto = null!;

            // act
            Action act = () => dto.ToEntity();

            // assert
            act.Should().Throw<NullReferenceException>();
        }

        [Test]
        public void UpdateEntity_CuandoFunkoEsNull_DeberiaLanzarNullReferenceException() {

            // arrange
            var dto = new UpdateFunkoDto("Thor", 29.99m, 3, "x", "y");

            // act
            Action act = () => dto.UpdateEntity(null!);

            // assert
            act.Should().Throw<NullReferenceException>();
        }
    }
}