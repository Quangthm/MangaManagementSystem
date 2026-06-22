package demo;

import java.util.Arrays;
import java.util.Objects;

/**
 * FixedExample - corrected version of BuggyExample.
 *
 * Every mistake from BuggyExample has been fixed. SpotBugs spotbugs:check
 * goal is expected to PASS.
 */
public class FixedExample {

    private String studentName;
    private int[] grades;

    public FixedExample(String studentName, int[] grades) {
        this.studentName = studentName;
        // FIX: defensive copy of external array
        this.grades = (grades == null) ? new int[0] : Arrays.copyOf(grades, grades.length);
    }

    // FIX: returns defensive copy instead of internal array
    public int[] getGrades() {
        return Arrays.copyOf(grades, grades.length);
    }

    // FIX: handles null safely before dereferencing
    public String formatStudentName(String prefix) {
        if (prefix == null) {
            return studentName;
        }
        return prefix.trim() + " " + studentName;
    }

    // FIX: uses .equals() for string comparison
    public boolean hasHonorsTitle() {
        String title = getTitle();
        return "Honors".equals(title);
    }

    // FIX: both equals() and hashCode() are implemented
    @Override
    public boolean equals(Object obj) {
        if (this == obj) return true;
        if (obj == null || getClass() != obj.getClass()) return false;
        FixedExample other = (FixedExample) obj;
        return Objects.equals(studentName, other.studentName);
    }

    @Override
    public int hashCode() {
        return Objects.hash(studentName);
    }

    private String getTitle() {
        return "Honors";
    }

    public String getStudentName() {
        return studentName;
    }

    public static void main(String[] args) {
        int[] grades = {85, 90, 78};
        FixedExample student = new FixedExample("Alice", grades);
        System.out.println("Student: " + student.getStudentName());
        System.out.println("Grades: " + Arrays.toString(student.getGrades()));
        System.out.println("Has honors: " + student.hasHonorsTitle());
    }
}
