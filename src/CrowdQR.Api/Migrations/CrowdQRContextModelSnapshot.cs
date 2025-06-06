﻿// <auto-generated />
using System;
using CrowdQR.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CrowdQR.Api.Migrations
{
    [DbContext(typeof(CrowdQRContext))]
    partial class CrowdQRContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("CrowdQR.Api.Models.Event", b =>
                {
                    b.Property<int>("EventId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("EventID");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("EventId"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("DjUserId")
                        .HasColumnType("integer")
                        .HasColumnName("DJUserID");

                    b.Property<bool>("IsActive")
                        .HasColumnType("boolean");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.HasKey("EventId");

                    b.HasIndex("DjUserId");

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Event", (string)null);
                });

            modelBuilder.Entity("CrowdQR.Api.Models.Request", b =>
                {
                    b.Property<int>("RequestId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("RequestID");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("RequestId"));

                    b.Property<string>("ArtistName")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("EventId")
                        .HasColumnType("integer")
                        .HasColumnName("EventID");

                    b.Property<string>("SongName")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("UserId")
                        .HasColumnType("integer")
                        .HasColumnName("UserID");

                    b.HasKey("RequestId");

                    b.HasIndex("EventId");

                    b.HasIndex("Status");

                    b.HasIndex("UserId");

                    b.ToTable("Request", (string)null);
                });

            modelBuilder.Entity("CrowdQR.Api.Models.Session", b =>
                {
                    b.Property<int>("SessionId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("SessionID");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("SessionId"));

                    b.Property<string>("ClientIP")
                        .HasMaxLength(45)
                        .HasColumnType("character varying(45)");

                    b.Property<int>("EventId")
                        .HasColumnType("integer")
                        .HasColumnName("EventID");

                    b.Property<DateTime>("LastSeen")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("RequestCount")
                        .HasColumnType("integer");

                    b.Property<int>("UserId")
                        .HasColumnType("integer")
                        .HasColumnName("UserID");

                    b.HasKey("SessionId");

                    b.HasIndex("UserId");

                    b.HasIndex("EventId", "UserId")
                        .IsUnique()
                        .HasDatabaseName("one_session_per_user_event");

                    b.ToTable("Session", (string)null);
                });

            modelBuilder.Entity("CrowdQR.Api.Models.TrackMetadata", b =>
                {
                    b.Property<int>("TrackId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("TrackId"));

                    b.Property<string>("AlbumArtUrl")
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<int?>("Duration")
                        .HasColumnType("integer");

                    b.Property<int>("RequestId")
                        .HasColumnType("integer");

                    b.Property<string>("SpotifyId")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("YoutubeId")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.HasKey("TrackId");

                    b.HasIndex("RequestId")
                        .IsUnique();

                    b.ToTable("TrackMetadata");
                });

            modelBuilder.Entity("CrowdQR.Api.Models.User", b =>
                {
                    b.Property<int>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("UserID");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("UserId"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Email")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<DateTime?>("EmailTokenExpiry")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("EmailVerificationToken")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<bool>("IsEmailVerified")
                        .HasColumnType("boolean");

                    b.Property<string>("PasswordHash")
                        .HasMaxLength(512)
                        .HasColumnType("character varying(512)");

                    b.Property<string>("PasswordSalt")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.HasKey("UserId");

                    b.HasIndex("Username")
                        .IsUnique();

                    b.ToTable("User", (string)null);
                });

            modelBuilder.Entity("CrowdQR.Api.Models.Vote", b =>
                {
                    b.Property<int>("VoteId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("VoteID");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("VoteId"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("RequestId")
                        .HasColumnType("integer")
                        .HasColumnName("RequestID");

                    b.Property<int>("UserId")
                        .HasColumnType("integer")
                        .HasColumnName("UserID");

                    b.HasKey("VoteId");

                    b.HasIndex("RequestId");

                    b.HasIndex("UserId", "RequestId")
                        .IsUnique()
                        .HasDatabaseName("one_vote_per_user");

                    b.ToTable("Vote", (string)null);
                });

            modelBuilder.Entity("CrowdQR.Api.Models.Event", b =>
                {
                    b.HasOne("CrowdQR.Api.Models.User", "DJ")
                        .WithMany("HostedEvents")
                        .HasForeignKey("DjUserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("DJ");
                });

            modelBuilder.Entity("CrowdQR.Api.Models.Request", b =>
                {
                    b.HasOne("CrowdQR.Api.Models.Event", "Event")
                        .WithMany("Requests")
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CrowdQR.Api.Models.User", "User")
                        .WithMany("Requests")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Event");

                    b.Navigation("User");
                });

            modelBuilder.Entity("CrowdQR.Api.Models.Session", b =>
                {
                    b.HasOne("CrowdQR.Api.Models.Event", "Event")
                        .WithMany("Sessions")
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CrowdQR.Api.Models.User", "User")
                        .WithMany("Sessions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Event");

                    b.Navigation("User");
                });

            modelBuilder.Entity("CrowdQR.Api.Models.TrackMetadata", b =>
                {
                    b.HasOne("CrowdQR.Api.Models.Request", "Request")
                        .WithOne("TrackMetadata")
                        .HasForeignKey("CrowdQR.Api.Models.TrackMetadata", "RequestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Request");
                });

            modelBuilder.Entity("CrowdQR.Api.Models.Vote", b =>
                {
                    b.HasOne("CrowdQR.Api.Models.Request", "Request")
                        .WithMany("Votes")
                        .HasForeignKey("RequestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CrowdQR.Api.Models.User", "User")
                        .WithMany("Votes")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Request");

                    b.Navigation("User");
                });

            modelBuilder.Entity("CrowdQR.Api.Models.Event", b =>
                {
                    b.Navigation("Requests");

                    b.Navigation("Sessions");
                });

            modelBuilder.Entity("CrowdQR.Api.Models.Request", b =>
                {
                    b.Navigation("TrackMetadata");

                    b.Navigation("Votes");
                });

            modelBuilder.Entity("CrowdQR.Api.Models.User", b =>
                {
                    b.Navigation("HostedEvents");

                    b.Navigation("Requests");

                    b.Navigation("Sessions");

                    b.Navigation("Votes");
                });
#pragma warning restore 612, 618
        }
    }
}
