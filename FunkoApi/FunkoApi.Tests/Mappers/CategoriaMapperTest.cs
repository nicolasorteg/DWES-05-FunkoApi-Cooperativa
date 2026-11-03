using FluentAssertions;
using FunkoApi.Dto.Categoria;
using FunkoApi.Mapper;
using FunkoApi.Models;

namespace FunkoApi.Tests.Mappers;

[TestFixture]
[TestOf(typeof(CategoriaMapper))]
public class CategoriaMapperTest {

    [TestFixture]
    public class CasosCorrectos {

        private Categoria _categoria = null!;

        [SetUp]
        public void SetUp() {
            _categoria = new Categoria {
                Id = "dQw4w9WgXcQ",
                Nombre = "Marvel",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };
        }

        [Test]
        public void ToResponseDto_DeberiaMapearIdYNombre() {

            // act
            var res = _categoria.ToResponseDto();

            // assert
            res.Should().NotBeNull();
            res.Id.Should().Be("dQw4w9WgXcQ");
            res.Nombre.Should().Be("Marvel");
        }

        [Test]
        public void ToEntityFromCreateDto_DeberiaMapearNombreYAsignarCreatedAt() {

            // arrange
            var dto = new CreateCategoriaDto("DC Comics");

            // act
            var res = dto.ToEntity();

            // assert
            res.Nombre.Should().Be("DC Comics");
            res.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            res.UpdatedAt.Should().BeNull();
            res.IsDeleted.Should().BeFalse();
            res.DeletedAt.Should().BeNull();
        }

        [Test]
        public void ToEntityFromCreateDto_NoDeberiaAsignarId() {

            // arrange
            var dto = new CreateCategoriaDto("DC Comics");

            // act
            var res = dto.ToEntity();

            // assert (el id estilo yt lo genera el servicio)
            res.Id.Should().BeNull();
        }

        [Test]
        public void UpdateEntity_DeberiaActualizarNombreYUpdatedAt() {

            // arrange
            var dto = new UpdateCategoriaDto("Star Wars");

            // act
            dto.UpdateEntity(_categoria);

            // assert
            _categoria.Nombre.Should().Be("Star Wars");
            _categoria.UpdatedAt.Should().NotBeNull();
            _categoria.UpdatedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Test]
        public void UpdateEntity_NoDeberiaModificarIdNiCreatedAt() {

            // arrange
            var dto = new UpdateCategoriaDto("Star Wars");
            var createdAt = _categoria.CreatedAt;

            // act
            dto.UpdateEntity(_categoria);

            // assert
            _categoria.Id.Should().Be("dQw4w9WgXcQ");
            _categoria.CreatedAt.Should().Be(createdAt);
        }
    }

    [TestFixture]
    public class CasosIncorrectos {

        [Test]
        public void ToResponseDto_CuandoCategoriaEsNull_DeberiaLanzarNullReferenceException() {

            // arrange
            Categoria categoria = null!;

            // act
            Action act = () => categoria.ToResponseDto();

            // assert
            act.Should().Throw<NullReferenceException>();
        }

        [Test]
        public void ToEntityFromCreateDto_CuandoDtoEsNull_DeberiaLanzarNullReferenceException() {

            // arrange
            CreateCategoriaDto dto = null!;

            // act
            Action act = () => dto.ToEntity();

            // assert
            act.Should().Throw<NullReferenceException>();
        }

        [Test]
        public void UpdateEntity_CuandoCategoriaEsNull_DeberiaLanzarNullReferenceException() {

            // arrange
            var dto = new UpdateCategoriaDto("Star Wars");

            // act
            Action act = () => dto.UpdateEntity(null!);

            // assert
            act.Should().Throw<NullReferenceException>();
        }
    }
}