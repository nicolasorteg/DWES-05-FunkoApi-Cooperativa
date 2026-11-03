using FluentAssertions;
using FunkoApi.Dto.Proveedor;
using FunkoApi.Mapper;
using FunkoApi.Models;
using MongoDB.Bson;

namespace FunkoApi.Tests.Mappers;

[TestFixture]
[TestOf(typeof(ProveedorMapper))]
public class ProveedorMapperTest {

    [TestFixture]
    public class CasosCorrectos {

        private Proveedor _proveedor = null!;

        [SetUp]
        public void SetUp() {
            _proveedor = new Proveedor {
                Id = new ObjectId("507f1f77bcf86cd799439011"),
                Nombre = "Funko Inc",
                Email = "contacto@funko.com",
                Telefono = "600123456",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };
        }

        // ---------- ToResponseDto ----------

        [Test]
        public void ToResponseDto_DeberiaConvertirObjectIdATextoYMapearCampos() {

            // act
            var res = _proveedor.ToResponseDto();

            // assert
            res.Id.Should().Be("507f1f77bcf86cd799439011");
            res.Nombre.Should().Be("Funko Inc");
            res.Email.Should().Be("contacto@funko.com");
            res.Telefono.Should().Be("600123456");
        }

        [Test]
        public void ToResponseDto_ConTelefonoNull_DeberiaDevolverTelefonoNull() {

            // arrange
            _proveedor.Telefono = null;

            // act
            var res = _proveedor.ToResponseDto();

            // assert
            res.Telefono.Should().BeNull();
        }

        // ---------- ToEntity ----------

        [Test]
        public void ToEntityFromCreateDto_DeberiaMapearCamposYAsignarCreatedAt() {

            // arrange
            var dto = new CreateProveedorDto("Funko Inc", "contacto@funko.com", "600123456");

            // act
            var res = dto.ToEntity();

            // assert
            res.Nombre.Should().Be("Funko Inc");
            res.Email.Should().Be("contacto@funko.com");
            res.Telefono.Should().Be("600123456");
            res.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            res.IsDeleted.Should().BeFalse();
            res.DeletedAt.Should().BeNull();
        }

        [Test]
        public void ToEntityFromCreateDto_DeberiaDejarUpdatedAtANull() {

            // arrange
            var dto = new CreateProveedorDto("Funko Inc", "contacto@funko.com", null);

            // act
            var res = dto.ToEntity();

            // assert
            res.UpdatedAt.Should().BeNull();
            res.Telefono.Should().BeNull();
        }

        // ---------- UpdateEntity (PUT completo) ----------

        [Test]
        public void UpdateEntity_DeberiaReemplazarTodosLosCamposYActualizarUpdatedAt() {

            // arrange
            var dto = new UpdateProveedorDto("Nuevo Nombre", "nuevo@funko.com", "699999999");

            // act
            dto.UpdateEntity(_proveedor);

            // assert
            _proveedor.Nombre.Should().Be("Nuevo Nombre");
            _proveedor.Email.Should().Be("nuevo@funko.com");
            _proveedor.Telefono.Should().Be("699999999");
            _proveedor.UpdatedAt.Should().NotBeNull();
            _proveedor.UpdatedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }
        

        [Test]
        public void UpdateEntity_NoDeberiaModificarIdNiCreatedAt() {

            // arrange
            var dto = new UpdateProveedorDto("Nuevo Nombre", "nuevo@funko.com", "699999999");
            var id = _proveedor.Id;
            var createdAt = _proveedor.CreatedAt;

            // act
            dto.UpdateEntity(_proveedor);

            // assert
            _proveedor.Id.Should().Be(id);
            _proveedor.CreatedAt.Should().Be(createdAt);
        }
    }

    [TestFixture]
    public class CasosIncorrectos {

        [Test]
        public void ToResponseDto_CuandoProveedorEsNull_DeberiaLanzarNullReferenceException() {

            // arrange
            Proveedor proveedor = null!;

            // act
            Action act = () => proveedor.ToResponseDto();

            // assert
            act.Should().Throw<NullReferenceException>();
        }

        [Test]
        public void ToEntityFromCreateDto_CuandoDtoEsNull_DeberiaLanzarNullReferenceException() {

            // arrange
            CreateProveedorDto dto = null!;

            // act
            Action act = () => dto.ToEntity();

            // assert
            act.Should().Throw<NullReferenceException>();
        }

        [Test]
        public void UpdateEntity_CuandoProveedorEsNull_DeberiaLanzarNullReferenceException() {

            // arrange
            var dto = new UpdateProveedorDto("Nuevo Nombre", "a@b.com", "");

            // act
            Action act = () => dto.UpdateEntity(null!);

            // assert
            act.Should().Throw<NullReferenceException>();
        }
    }
}