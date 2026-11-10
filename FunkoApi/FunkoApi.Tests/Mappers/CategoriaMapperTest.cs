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
            var res = _categoria.ToResponseDto();

            res.Should().NotBeNull();
            res.Id.Should().Be("dQw4w9WgXcQ");
            res.Nombre.Should().Be("Marvel");
        }

        [Test]
        public void ToEntityFromCreateDto_DeberiaMapearNombreYAsignarCreatedAt() {
            var dto = new CreateCategoriaDto("DC Comics");

            var res = dto.ToEntity();

            res.Nombre.Should().Be("DC Comics");
            res.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            res.UpdatedAt.Should().BeNull();
            res.IsDeleted.Should().BeFalse();
            res.DeletedAt.Should().BeNull();
        }

        [Test]
        public void ToEntityFromCreateDto_NoDeberiaAsignarId() {
            var dto = new CreateCategoriaDto("DC Comics");

            var res = dto.ToEntity();

            // el id estilo yt lo genera el ValueGenerator al guardar
            res.Id.Should().BeNullOrEmpty();
        }

        [Test]
        public void UpdateEntity_DeberiaDevolverEntidadNuevaConElNombre() {
            var dto = new UpdateCategoriaDto("Star Wars");

            var res = dto.UpdateEntity();

            res.Nombre.Should().Be("Star Wars");
            res.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            res.IsDeleted.Should().BeFalse();
        }

        [Test]
        public void UpdateEntity_NoDeberiaModificarOtraCategoria() {
            var dto = new UpdateCategoriaDto("Star Wars");

            var res = dto.UpdateEntity();

            res.Should().NotBeSameAs(_categoria);
            _categoria.Nombre.Should().Be("Marvel");
        }
    }

    [TestFixture]
    public class CasosIncorrectos {

        [Test]
        public void ToResponseDto_CuandoCategoriaEsNull_DeberiaLanzarNullReferenceException() {
            Categoria categoria = null!;

            Action act = () => categoria.ToResponseDto();

            act.Should().Throw<NullReferenceException>();
        }

        [Test]
        public void ToEntityFromCreateDto_CuandoDtoEsNull_DeberiaLanzarNullReferenceException() {
            CreateCategoriaDto dto = null!;

            Action act = () => dto.ToEntity();

            act.Should().Throw<NullReferenceException>();
        }

        [Test]
        public void UpdateEntity_CuandoDtoEsNull_DeberiaLanzarNullReferenceException() {
            UpdateCategoriaDto dto = null!;

            Action act = () => dto.UpdateEntity();

            act.Should().Throw<NullReferenceException>();
        }
    }
}