package hn.uth.bitacora;

import android.content.Intent;
import android.os.Bundle;
import android.util.Patterns;
import android.view.View;
import android.view.inputmethod.EditorInfo;
import android.widget.TextView;
import androidx.appcompat.app.AppCompatActivity;
import com.google.android.material.button.MaterialButton;
import com.google.android.material.textfield.TextInputEditText;
import com.google.android.material.textfield.TextInputLayout;
import java.io.IOException;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public final class LoginActivity extends AppCompatActivity {
    private TextInputEditText emailInput, passwordInput;
    private TextInputLayout emailLayout, passwordLayout;
    private TextView message;
    private MaterialButton loginButton;
    private View progress;
    private SessionManager session;
    private final ApiClient api = new ApiClient();
    private final ExecutorService executor = Executors.newSingleThreadExecutor();
    private boolean busy;

    @Override protected void onCreate(Bundle state) {
        super.onCreate(state);
        session = new SessionManager(this);
        if (session.isValid()) { openMain(); return; }
        boolean expired = !session.token().isEmpty() || getIntent().getBooleanExtra("expired", false);
        session.clear();
        setContentView(R.layout.activity_login);
        ScreenInsets.apply(this);
        emailInput = findViewById(R.id.emailInput);
        passwordInput = findViewById(R.id.passwordInput);
        emailLayout = findViewById(R.id.emailLayout);
        passwordLayout = findViewById(R.id.passwordLayout);
        message = findViewById(R.id.message);
        loginButton = findViewById(R.id.loginButton);
        progress = findViewById(R.id.progress);
        loginButton.setOnClickListener(view -> login());
        passwordInput.setOnEditorActionListener((view, action, event) -> {
            if (action == EditorInfo.IME_ACTION_DONE) { login(); return true; }
            return false;
        });
        if (expired) showMessage(R.string.expired);
    }

    private void login() {
        if (busy) return;
        emailLayout.setError(null);
        passwordLayout.setError(null);
        message.setVisibility(View.GONE);
        String email = emailInput.getText() == null ? "" : emailInput.getText().toString().trim();
        String password = passwordInput.getText() == null ? "" : passwordInput.getText().toString();
        boolean valid = true;
        if (email.isEmpty()) { emailLayout.setError(getString(R.string.email_required)); valid = false; }
        else if (!Patterns.EMAIL_ADDRESS.matcher(email).matches()) {
            emailLayout.setError(getString(R.string.email_invalid)); valid = false;
        }
        if (password.isEmpty()) { passwordLayout.setError(getString(R.string.password_required)); valid = false; }
        if (!valid) {
            if (emailLayout.getError() != null) emailInput.requestFocus();
            else passwordInput.requestFocus();
            return;
        }
        if (BuildConfig.API_BASE_URL.isEmpty()) { showMessage(R.string.config_missing); return; }
        setBusy(true);
        executor.execute(() -> {
            try {
                ApiClient.Result result = api.login(email, password);
                runOnUiThread(() -> {
                    if (isDestroyed() || isFinishing()) return;
                    setBusy(false);
                    if (result.code == 200) {
                        try {
                            session.save(result.body);
                            passwordInput.setText("");
                            openMain();
                        } catch (Exception exception) {
                            session.clear();
                            showMessage(R.string.server_error);
                        }
                    } else if (result.code == 401) showMessage(R.string.credentials_error);
                    else showMessage(R.string.server_error);
                });
            } catch (Exception exception) {
                int error = exception instanceof IOException ? R.string.connection_error : R.string.server_error;
                runOnUiThread(() -> {
                    if (isDestroyed() || isFinishing()) return;
                    setBusy(false);
                    showMessage(error);
                });
            }
        });
    }

    private void setBusy(boolean loading) {
        busy = loading;
        loginButton.setEnabled(!loading);
        emailInput.setEnabled(!loading);
        passwordInput.setEnabled(!loading);
        passwordLayout.setEndIconVisible(!loading);
        loginButton.setText(loading ? R.string.loading : R.string.login);
        progress.setVisibility(loading ? View.VISIBLE : View.GONE);
    }

    private void showMessage(int resource) {
        message.setText(resource);
        message.setVisibility(View.VISIBLE);
    }

    private void openMain() {
        startActivity(new Intent(this, MainActivity.class)
                .addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK));
        finish();
    }

    @Override protected void onDestroy() {
        executor.shutdownNow();
        api.cancel();
        super.onDestroy();
    }
}
