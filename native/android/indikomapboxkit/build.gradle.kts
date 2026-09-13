import org.gradle.api.artifacts.component.ModuleComponentIdentifier

plugins {
    id("com.android.library")
    id("org.jetbrains.kotlin.android")
}

android {
    namespace = "ch.indiko.mapbox"
    compileSdk = 35

    defaultConfig {
        minSdk = 30
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlinOptions {
        jvmTarget = "17"
    }
}

dependencies {
    api("com.mapbox.maps:android:11.30.1")
}

// Exports the com.mapbox.* runtime AARs/JARs so the .NET binding project can
// reference them directly (Bind=false) without needing Maven auth on the .NET side.
tasks.register<Copy>("exportMapboxDeps") {
    from({
        configurations.getByName("releaseRuntimeClasspath").incoming.artifactView {
            componentFilter { id ->
                id is ModuleComponentIdentifier && id.group.startsWith("com.mapbox")
            }
        }.files
    })
    into(layout.buildDirectory.dir("exported-deps"))
}
