import java.util.zip.ZipEntry
import java.util.zip.ZipOutputStream
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

// Exports the com.mapbox.* runtime AARs so the .NET binding project can reference
// them directly (Bind=false) without needing Maven auth on the .NET side.
// Plain JARs are wrapped into minimal AARs: the .NET Android build packages
// Bind=false AARs into the app but silently drops plain JARs from the dex.
tasks.register("exportMapboxDeps") {
    val depFiles = configurations.getByName("releaseRuntimeClasspath").incoming.artifactView {
        componentFilter { id ->
            id is ModuleComponentIdentifier &&
                (id.group.startsWith("com.mapbox") ||
                    // cronet-api classes must be present so MapboxCommon's HTTP stack can
                    // detect that no Cronet provider is installed and fall back to OkHttp.
                    id.group == "org.chromium.net")
        }
    }.files
    val outDir = layout.buildDirectory.dir("exported-deps")

    doLast {
        val out = outDir.get().asFile
        out.deleteRecursively()
        out.mkdirs()

        depFiles.forEach { file ->
            when
            {
                file.name.endsWith(".aar") -> file.copyTo(File(out, file.name), overwrite = true)
                file.name.endsWith(".jar") ->
                {
                    val base = file.name.removeSuffix(".jar")
                    val packageName = "wrapped." + base.replace(Regex("[^A-Za-z0-9]"), "_")
                    val manifest = "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\" " +
                        "package=\"$packageName\" />"
                    ZipOutputStream(File(out, "$base.aar").outputStream()).use { zip ->
                        zip.putNextEntry(ZipEntry("AndroidManifest.xml"))
                        zip.write(manifest.toByteArray())
                        zip.closeEntry()
                        zip.putNextEntry(ZipEntry("classes.jar"))
                        file.inputStream().use { input -> input.copyTo(zip) }
                        zip.closeEntry()
                    }
                }
            }
        }
    }
}
